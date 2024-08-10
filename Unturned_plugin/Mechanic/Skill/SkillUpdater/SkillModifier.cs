using Nekos.SpecialtyPlugin.CustomEvent;
using Nekos.SpecialtyPlugin.Mechanic.Autoload;
using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillMultipliers;
using Nekos.SpecialtyPlugin.Mechanic.Skill.SkillsetRequirementTypes;
using Nekos.SpecialtyPlugin.Persistance;
using Nekos.SpecialtyPlugin.Watcher;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Collections.Generic;

namespace Nekos.SpecialtyPlugin.Mechanic.Skill{
  public partial class SkillUpdater {
    private class SkillModifier: ISkillModifier {
      private readonly SkillConfig _config;
      private readonly ISkillPersistance _persistance;
      private readonly UnturnedUser _user;
      private readonly PlayerContext? _playerContext;


      public int ExcessExp {
        get {
          return _persistance.ExpData.excess_exp;
        }

        set {
          _persistance.ExpData.excess_exp = value;
        }
      }


      private void _checkCalculate(EPlayerSpeciality spec, byte skillidx) {
        int _currentexp = _persistance.ExpData.skillsets_exp[(byte)spec][skillidx];
        int _borderlow = _persistance.ExpData.skillsets_expborderlow[(byte)spec][skillidx];
        int _borderHigh = _persistance.ExpData.skillsets_expborderhigh[(byte)spec][skillidx];

        if(_currentexp < _borderlow || _currentexp >= Math.Abs(_borderHigh))
          _config.Calculation.ReCalculateSkillTo(_user, _persistance.ExpData, (byte)spec, skillidx);
      }


      public SkillModifier(ISkillPersistance persistance, UnturnedUser user) {
        _persistance = persistance;
        _user = user;
        SkillConfig? _config = SpecialtyOverhaul.Instance?.SkillConfigInstance;
        if(_config == null)
          throw new ApplicationException("Plugin isn't yet started.");

        this._config = _config;

        _playerContext = PlayerLookoutContext.GetPlayerContext(user.Player);
        if(_playerContext == null)
          SpecialtyOverhaul.Instance?.PrintToError(string.Format("Player Context for ID: {0}, not found.", user.SteamId.m_SteamID));
      }


      public float Level(EPlayerSpeciality spec, byte index, float level = -1) {
        ICalculationUtils calcutil = _config.Calculation;
        // if setter
        if(level >= 0) {
          _persistance.ExpData.skillsets_exp[(byte)spec][index] = calcutil.CalculateLevelExp(_persistance.ExpData, spec, index, level);
          _persistance.ExpData.skillsets_exp_fraction[(byte)spec][index] = 0;

          _checkCalculate(spec, index);
        }

        return calcutil.CalculateLevelFloat(_persistance.ExpData, spec, index);
      }

      public int LevelExp(EPlayerSpeciality spec, byte index, int exp = -1) {
        // if setter
        if(exp >= 0) {
          _persistance.ExpData.skillsets_exp[(byte)spec][index] = exp;
          _persistance.ExpData.skillsets_exp_fraction[(byte)spec][index] = 0;

          _checkCalculate(spec, index);
        }

        return _persistance.ExpData.skillsets_exp[(byte)spec][index];
      }

      public void ExpFractionIncrement(EPlayerSpeciality spec, byte index, float increment) {
        _playerContext?.IterateEnvironmentFlag((EEnvironment eEnvironment) => {
          ISkillMult_Environment skillMult = _config.GetEnvironmentMult(eEnvironment);

          increment *= skillMult.GetMultiplier(SkillConfig.EModifierType.GAIN, spec, index);
        });

        float _fraction = _persistance.ExpData.skillsets_exp_fraction[(byte)spec][index] + increment;
        if(_fraction > 1) {
          int _expin = (int)Math.Floor(_fraction); _fraction -= _expin;
          _persistance.ExpData.skillsets_exp[(byte)spec][index] += _expin;
          _checkCalculate(spec, index);
        }

        _persistance.ExpData.skillsets_exp_fraction[(byte)spec][index] = _fraction;

        if(spec == EPlayerSpeciality.SUPPORT && index == (byte)EPlayerSupport.OUTDOORS)
          ExpFractionIncrement(EPlayerSpeciality.SUPPORT, (byte)EPlayerSupport.FISHING, increment);
      }

      public bool SetPlayerSkillset(EPlayerSkillset skillset, bool force = false) {
        SkillConfig? config = SpecialtyOverhaul.Instance?.SkillConfigInstance;
        if(config == null)
          return false;

        ISkillsetRequirement skillsetRequirement = config.GetSkillsetRequirement(skillset);
        bool _isfullfilled = skillsetRequirement.IsRequirementsFulfilled(_persistance.ExpData);

        ICalculationUtils calcutil = config.Calculation;

        bool _res = true;
        if(_isfullfilled) {
          _persistance.ExpData.skillset = skillset;
          calcutil.ReCalculateAllSkillTo(_user, _persistance.ExpData);
        }
        else {
          if(force) {
            skillsetRequirement.ForceRequirements(_persistance.ExpData);
            calcutil.ReCalculateAllSkillTo(_user, _persistance.ExpData);
          }
          else
            _res = false;
        }

        return _res;
      }

      public EPlayerSkillset GetPlayerSkillset() {
        return _persistance.ExpData.skillset;
      }

      public List<(ESkillsetRequirementType, object)> GetSkillsetRequirement(EPlayerSkillset skillset) {
        ISkillsetRequirement req = _config.GetSkillsetRequirement(skillset);
        return req.ListOfPendingRequirement(_persistance.ExpData);
      }

      public bool IsSkillsetFulfilled(EPlayerSkillset skillset) {
        SkillConfig? config = SpecialtyOverhaul.Instance?.SkillConfigInstance;
        if(config == null)
          return false;

        ISkillsetRequirement skillsetRequirement = config.GetSkillsetRequirement(skillset);
        return skillsetRequirement.IsRequirementsFulfilled(_persistance.ExpData);
      }

      public bool PurchaseRandomBoost() {
        int _cost = _config.GetRandomBoostCost();
        if(ExcessExp >= _cost) {
          ExperienceWatcher.DisableWatch(_user.Player.SteamId.m_SteamID);

          _user.Player.Player.skills.ServerSetExperience(25);
          _user.Player.Player.skills.ReceiveBoostRequest();

          ExcessExp -= _cost;
          ExperienceWatcher.EnableWatch(_user.Player.SteamId.m_SteamID);

          return true;
        }

        return false;
      }
    }
  }
}
