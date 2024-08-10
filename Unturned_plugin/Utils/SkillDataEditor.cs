using Nekos.SpecialtyPlugin.Mechanic.Skill;
using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillsetRequirementTypes;
using Nekos.SpecialtyPlugin.Persistance;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;

using static Nekos.SpecialtyPlugin.Mechanic.Skill.PreviouslyModifiedSkillData;

namespace Nekos.SpecialtyPlugin.Utils {
  public partial class SkillDataEditor: ISkillModifier {
    public readonly static int Default_MaxUndo = 20;


    private enum _changeStepCode {
      CANNOT_REDO,
      CANNOT_UNDO,
      SUCCESS
    }


    private readonly ISkillPersistance _persistance;
    public ISkillPersistance SkillPersistance {
      get {
        return _persistance; 
      } 
    }

    private readonly SkillConfig _config;
    private readonly LinkedList<PreviouslyModifiedSkillData> _steps = new();

    private SpecialtyExpData _tempExpData;

    private int _steps_index = 0;
    private LinkedListNode<PreviouslyModifiedSkillData> _currentStep = new(new());

    private int _maxUndo;


    /// <summary>
    /// To get or set player's ExcessExp.
    /// </summary>
    public int ExcessExp {
      get {
        return _tempExpData.excess_exp;
      }

      set {
        PreviouslyModifiedSkillData _pmsd = new();
        _pmsd.ModifiedData.Add((
          ChangeCodes.TYPE_EXCESS,
          new ChangeExcessExp() {
            ExcessExp = _tempExpData.excess_exp
          }
        ));

        _tempExpData.excess_exp = value;

        _addStep(_pmsd);
      }
    }


    /// <summary>
    /// Note: This function will always set _steps_index to _steps.Count.
    /// </summary>
    /// <param name="pmsd"></param>
    private void _addStep(PreviouslyModifiedSkillData pmsd) {
      if(_steps_index < _steps.Count)
        for(int i = _steps_index; i < _steps.Count; i++)
          _steps.RemoveLast();

      _steps.AddLast(pmsd);
      if(_steps.Count > _maxUndo)
        _steps.RemoveFirst();

      _steps_index = _steps.Count;
      _currentStep = _steps.Last;
    }

    private _changeStepCode _changeStepOffset(int offset) {
      if(offset != 0) {
        int _newindex = offset + _steps_index;

        if(_newindex >= _steps.Count)
          return _changeStepCode.CANNOT_REDO;
        else if(_newindex < 0)
          return _changeStepCode.CANNOT_UNDO;

        int _delta = Math.Abs(offset);
        if(_steps_index == _steps.Count) {
          var _nowStep = _currentStep.Value.SetToData(ref _tempExpData);
          _addStep(_nowStep);

          _currentStep = _currentStep.Previous;

          _steps_index -= 2;
          _delta--;
        }

        for(int i = 0; i < _delta; i++) {
          _currentStep = _currentStep.Previous;
          _currentStep.Value.SetToData(ref _tempExpData);
        }
      }

      return _changeStepCode.SUCCESS;
    }


    public SkillDataEditor(ISkillPersistance persistance) {
      SkillConfig? _skillConfig = SpecialtyOverhaul.Instance?.SkillConfigInstance;
      if(_skillConfig == null)
        throw new ApplicationException("Plugin isn't initialized yet.");

      _persistance = persistance;
      _tempExpData = new(persistance.ExpData);
      _config = _skillConfig;

      SetMaxUndo(Default_MaxUndo);
    }


    public void SetMaxUndo(int maxUndo) {
      // + 1 for current state
      _maxUndo = maxUndo + 1;
    }

    public void ApplyData() {
      _persistance.ExpData.SetTo(_tempExpData);

      UnturnedUser? user = SpecialtyOverhaul.Instance?.UnturnedUserProviderInstance.GetUser(_persistance.PlayerID.steamID);

      if(user != null)
        _config.Calculation.ReCalculateAllSkillTo(user, _persistance.ExpData);
    }

    public void SaveData() {
      ApplyData();
      _persistance.Save();
    }

    public bool UndoData() {
      switch(_changeStepOffset(-1)) {
        case _changeStepCode.SUCCESS:
          return true;

        default:
          return false;
      }
    }

    public bool RedoData() {
      switch(_changeStepOffset(1)) {
        case _changeStepCode.SUCCESS:
          return true;

        default:
          return false;
      }
    }

    public void EraseData() {
      _persistance.Erase();
      ApplyData();
    }

    /// <summary>
    /// To get or set player's skill level. If the 'level' parameter is not set, then the function will be as getter.
    /// </summary>
    /// <param name="spec"></param>
    /// <param name="index"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public float Level(EPlayerSpeciality spec, byte index, float level = -1) {
      // if setter
      if(level >= 0)
        LevelExp(spec, index, _config.Calculation.CalculateLevelExp(_tempExpData, spec, index, level));

      return _config.Calculation.CalculateLevelFloat(_tempExpData, spec, index);
    }

    /// <summary>
    /// To get or set player's skill exp. If the 'exp' parameter is not set, then the function will be as getter.
    /// </summary>
    /// <param name="spec"></param>
    /// <param name="index"></param>
    /// <param name="exp"></param>
    /// <returns></returns>
    public int LevelExp(EPlayerSpeciality spec, byte index, int exp = -1) {
      // if setter
      if(exp >= 0) {
        PreviouslyModifiedSkillData _pmsd = new();
        _pmsd.ModifiedData.Add((
          ChangeCodes.TYPE_EXP,
          new ChangeExp() {
            Speciality = spec,
            SkillIdx = index,
            Exp = _tempExpData.skillsets_exp[(byte)spec][index]
          }
        ));

        _tempExpData.skillsets_exp[(byte)spec][index] = exp;

        PreviouslyModifiedSkillData _newpmsd = _config.Calculation.ReCalculateSkillTo(null, _tempExpData, (byte)spec, index);

        _pmsd.CombineWithAnother(_newpmsd);

        _addStep(_pmsd);
      }

      return _tempExpData.skillsets_exp[(byte)spec][index];
    }

    /// <summary>
    /// (This is not used.)
    /// <para></para>
    /// This function is to increment skill level using floating point.
    /// </summary>
    /// <param name="spec"></param>
    /// <param name="index"></param>
    /// <param name="increment">Level to increment</param>
    public void ExpFractionIncrement(EPlayerSpeciality spec, byte index, float increment) {
      // There's no reason to put a function for this, since it would be useless for a data editor.
      // This function exist just to fulfill ISkillModifier interface requirements.
    }

    /// <summary>
    /// This should set player's skillset, since changing skillset needs some minimum levels (or requirements) to be sufficient enough.
    /// </summary>
    /// <param name="skillset">The skillset that will be applied to the player</param>
    /// <param name="force">Will force the skillset, even if the requirements aren't fulfilled.</param>
    /// <returns>If successfully set the skillset.</returns>
    public bool SetPlayerSkillset(EPlayerSkillset skillset, bool force = false) {
      var skillsetReq = _config.GetSkillsetRequirement(skillset);
      if(skillsetReq.IsRequirementsFulfilled(_tempExpData) || force) {
        var _pmsd = skillsetReq.ForceRequirements(_tempExpData);
        _addStep(_pmsd);

        return true;
      }

      return false;
    }

    /// <summary>
    /// To get player's current <see cref="EPlayerSpeciality"/>.
    /// </summary>
    /// <returns></returns>
    public EPlayerSkillset GetPlayerSkillset() {
      return _tempExpData.skillset;
    }

    public List<(ESkillsetRequirementType, object)> GetSkillsetRequirement(EPlayerSkillset skillset) {
      ISkillsetRequirement req = _config.GetSkillsetRequirement(skillset);
      return req.ListOfPendingRequirement(_persistance.ExpData);
    }

    /// <summary>
    /// To check if the player can apply to certain skillset.
    /// </summary>
    /// <param name="skillset"></param>
    /// <returns>If applicable</returns>
    public bool IsSkillsetFulfilled(EPlayerSkillset skillset) {
      var skillsetReq = _config.GetSkillsetRequirement(skillset);
      return skillsetReq.IsRequirementsFulfilled(_tempExpData);
    }

    /// <summary>
    /// (This is not used.)
    /// <para></para>
    /// To purchase a random boost.
    /// </summary>
    /// <returns>If successful or not. If unsuccessful, then ExcessExp isn't enough.</returns>
    public bool PurchaseRandomBoost() {
      // As of now, supplying random boosts through Unturned's API is not possible.
      return false;
    }
  }
}
