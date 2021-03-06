using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FantasyEngine;
//战斗段元

public class BattleUnitInitParam : VisualObjectInitParam
{

}

public enum AttachMountType : uint
{
    Wing,
    Weapon,
    AttachCount,
}


public abstract class BattleUnit : VisualObject
{
    private RoleProperty mProperty = null;
    private BattleUnitActionCenter mActionCenter = null;
    //技能容器
    protected BattleUnitSkillContainer mSkillContainer = null;

    //buff容器.
    protected SkillEffectManager mSkillEffectManager = null;

    // 动态标记.
    protected ActiveFlagsContainer mActiveFlagsContainer = null;

    // 随机事件容器.
    protected BattleUnitRandEventContainer mRandEventContainer = null;

    protected BattleUnitAI mBattleUintAI = null;

    private Vector3f mHomePosition = new Vector3f();

    private LeagueDef mLeague = LeagueDef.InvalidLeague;

    private HitMaterialEffectCdContainer mHitMaterialEffectCdContainer = null;

	private static readonly uint kCriticalStrikeEffectID = GameConfig.CriticalStrikeEffectID;

	/// <summary>
	/// 硬直CD.
	/// </summary>
	private uint mSpasticityCd = 0;

    private float mMoveDirection = 0.0f;

    //等待消失时间
    protected float mMaxWaitDisappearTime = 0.0f;
    //消失过程时间
    protected float mMaxDisappearTime = 0.0f;

    private float mWaitTime = 0.0f;
    private float mDisappearTime = 0.0f;

    private float mTwinkleTime = 0.0f;
    private const float twinkeLast = 500f;

    private BitArray mMtlFlags = new BitArray(8,false);

	private ObjectTaskBase mRageTask;

    private bool mWudi = false;

	/// <summary>
	/// 防护盾界面, 在单位的头上, 随着单位移动而移动.
	/// </summary>
	private UIShield mUIShield = null;

    enum MoveMode : int
    {
        MOVE_INVALID = -1,

        /** 跑 */
        MOVE_RUN = 0,

        /** 走 */
        MOVE_WALK = 1,

        /** 急跑 */
        MOVE_SCURRY = 2,

        MOVE_MAOYAO_RUN = 3,
    };

    enum MtlFlag
    {
        Rage,//狂暴
    }

    override public bool Init(ObjectInitParam param)
    {
        if (!base.Init(param))
            return false;

        mHomePosition.x = mPosition.x;
        mHomePosition.y = mPosition.y;
        mHomePosition.z = mPosition.z;

        mProperty = new RoleProperty();

        mActionCenter = new BattleUnitActionCenter(this);
        mSkillContainer = new BattleUnitSkillContainer();
        mSkillEffectManager = new SkillEffectManager(this);
        mActiveFlagsContainer = new ActiveFlagsContainer();
        mRandEventContainer = new BattleUnitRandEventContainer();

        mHitMaterialEffectCdContainer = new HitMaterialEffectCdContainer();

		// 默认透明度为100.
		SetBaseProperty((int)PropertyTypeEnum.PropertyTypeAlpha, 100);

		// 默认硬直抗性为100.
		SetBaseProperty((int)PropertyTypeEnum.PropertyTypeSpasticityResistance, 100);

		SetBaseProperty((int)PropertyTypeEnum.PropertyTypeScale_Rate, 100);

        return true;
    }

    override public void Destroy()
    {
        if(mBattleUintAI != null)
        {
            mBattleUintAI.Destory();
            mBattleUintAI = null;
        }

        if (mActionCenter != null)
        {
            mActionCenter.Destroy();
            mActionCenter = null;
        }

        if (mSkillEffectManager != null)
        {
            SkillDetails.OnSkillEffectOwnerEvent(mSkillEffectManager, SkillEffectOwnerEventDef.OwnerLeaveScene);
            mSkillEffectManager.Destroy();
            mSkillEffectManager = null;
        }

        mActiveFlagsContainer = null;
        mRandEventContainer = null;

        mHitMaterialEffectCdContainer = null;

        mProperty = null;

		mUIShield = null;

        base.Destroy();

 

        for (int i = 0; i < mAttachMents.Length; ++i)
        {
            AttachMent attach = mAttachMents[i];
            if (attach != null && attach.visual != null)
                attach.visual.Destroy();
            mAttachMents[i] = null;
        }

    }

	protected override void onModelLoaded(GameObject obj)
	{
		base.onModelLoaded(obj);
		if (mModelResID >= 0)
		{
			ModelTableItem modelRes = DataManager.ModelTable[mModelResID] as ModelTableItem;
			float initScale = modelRes.scale * 1000;
			float newScale = GetPropertyValue((int)PropertyTypeEnum.PropertyTypeScale_Rate) * initScale / 100f;
			if (!Mathf.Approximately(initScale, newScale))
				onPropertyChanged((int)PropertyTypeEnum.PropertyTypeScale_Rate, initScale, newScale);
		}
	}

	/// <summary>
	/// <para>
	/// 该函数返回true的条件为:
	/// </para>
	/// 1. 阵营关系上, this和other敌对.
	/// <para></para>
	/// 2. other可以被锁敌攻击(非神圣的), 但是不一定可以被添加技能效果.
	/// </summary>
    public bool IsEnemy(BattleUnit other)
    {
		if (other == null || other.IsInviolable())
			return false;

		return IsRivalCamps(other);
    }

	/// <summary>
	/// 阵营关系上, this和other是否是敌对.
	/// </summary>
	public bool IsRivalCamps(BattleUnit other) {
		return other != null 
			&& LeagueManager.GetRelationship(GetLeague(), other.GetLeague()) == LeagueRelationship.Enemy;
	}

	/// <summary>
	/// 变身, 改变模型.
	/// </summary>
	/// <param name="modelID">新的模型ID.</param>
	public ErrorCode Transform(int modelID)
	{
		if (modelID >= 0 && modelID != mModelResID && ChangeModel(modelID))
			AddActiveFlag(ActiveFlagsDef.Transformed, true, true);

		return ErrorCode.Succeeded;
	}

	/// <summary>
	/// 临时更改该单位的技能.
	/// </summary>
	/// <param name="newSkills">新的技能列表, 如果为null, 那么恢复最初的技能</param>
	public virtual ErrorCode SkillTransform(IEnumerable<Pair<uint, string>> newSkills)
	{
		ErrorHandler.Parse(ErrorCode.ConfigError, dbgGetIdentifier() + "未实现更换技能");
		return ErrorCode.ConfigError;
	}

	/// <summary>
	/// 取消变身(只是减少变身的计数, 只有当计数为0时, 才彻底取消变身).
	/// </summary>
	public ErrorCode UndoTransform()
	{
		// 未发生变身.
		if (mActiveFlagsContainer[ActiveFlagsDef.Transformed] == 0)
			return ErrorCode.Succeeded;

		AddActiveFlag(ActiveFlagsDef.Transformed, false, true);

		return ErrorCode.Succeeded;
	}

    /// <summary>
    /// 对targetPosition使用skill技能.
    /// </summary>
    private ErrorCode StartSkill(BattleUnitSkill skill, Vector3 targetPosition)
    {
        if (skill == null)
            return ErrorCode.InvalidParam;

        if (skill.IsRegularAttack)
        {
            if (!CanRegularAttack())
                return ErrorCode.UnableToAttack;
        }
        else if (!CanUseSkill())
            return ErrorCode.UnableToUseSkill;

        return SkillDetails.StartSkillAction(mActionCenter, skill, targetPosition);
    }

    public ErrorCode CheckSkillCost(int skillID)
    {
        if (mSkillContainer == null)
            return ErrorCode.InvalidParam;
        BattleUnitSkill skill = mSkillContainer.GetSkill(skillID);
		if (skill == null)
			return ErrorCode.InvalidParam;

        return SkillUtilities.CheckCost(this, skill);
    }

    /// <summary>
    /// 给当前单位添加技能效果.
    /// </summary>
    /// <param name="attackerAttr">为this添加技能效果的单位的属性</param>
    /// <param name="type">效果类型</param>
    /// <param name="resID">效果ID</param>
    /// <returns></returns>
    public ErrorCode AddSkillEffect(AttackerAttr attackerAttr, SkillEffectType type, uint resID)
    {
        // 是否死亡.
        if (IsDead())
            return ErrorCode.TargetIsDead;

		// 由单位的本身类型, 决定的是否免疫给类型的技能效果.
		if (SkillEffectImmunity(type))
			return ErrorCode.AddEffectFailedSkillEffectImmunity;

		if (SkillUtilities.IsHarmfulEffect(type, resID) && IsInviolable())
			return ErrorCode.AddEffectFailedSkillEffectImmunity;

        ErrorCode err = ErrorCode.ConfigError;
        switch (type)
        {
			case SkillEffectType.Spasticity:
				#region Pending
				err = SkillDetails.StartSpasticity(mActionCenter, attackerAttr, resID, ref mSpasticityCd);
				#endregion Pending
				break;
            case SkillEffectType.Displacement:
                err = SkillDetails.StartDisplace(mActionCenter, attackerAttr, resID);
                break;
            case SkillEffectType.Buff:
			case SkillEffectType.Impact:
                err = SkillDetails.StartSkillEffect(attackerAttr, mSkillEffectManager, type, resID);
                break;
			default:
				break;
        }

		if (err == ErrorCode.Succeeded && type == SkillEffectType.Buff)
			onSkillBuffAdded((uint)resID);
		
        return err;
    }

    /// <summary>
    /// 根据资源ID, 移除SkillBuff.
    /// </summary>
    public ErrorCode RemoveSkillBuffByResID(uint resID)
    {
		if (mSkillEffectManager != null)
		{
			mSkillEffectManager.ForEverySkillEffect(new SkillUtilities.KillSkillBuffByResource(resID));
			return ErrorCode.Succeeded;
		}

		return ErrorCode.LogicError;
    }

	/// <summary>
	/// 根据组ID, 移除skillbuff.
	/// </summary>
	public ErrorCode RemoveSkillBuffByGroupID(uint groupID)
	{
		if (mSkillEffectManager != null)
		{
			mSkillEffectManager.ForEverySkillEffect(new SkillUtilities.KillSkillBuffByGroup(groupID));
			return ErrorCode.Succeeded;
		}

		return ErrorCode.LogicError;
	}

    /// <summary>
    /// 添加randEventResourceID指定的skillrandevent资源.
    /// </summary>
	/// <param name="buffRes">来自哪个buff.</param>
	/// <param name="buffCreaterAttr">buff创建者的属性</param>
	public ErrorCode AddRandEvent(SkillBuffTableItem buffRes, AttackerAttr buffCreaterAttr)
    {
		return SkillDetails.AddRandEvent(this, mRandEventContainer, buffRes, buffCreaterAttr);
    }

	public ErrorCode RemoveRandEvent(SkillBuffTableItem buffRes, SkillEffectStopReason stopReason)
	{
		return SkillDetails.RemoveRandEvent(this, mRandEventContainer, buffRes, stopReason);
	}

    /// <summary>
    /// 增加活动标记的计数(+-1).
    /// </summary>
    /// <param name="increase">增加或者减少计数</param>
	/// <param name="impactAlreadyHappened">是否影响当前已经发生的Action, SkillEffect等</param>
	public void AddActiveFlag(ActiveFlagsDef flagName, bool increase, bool impactAlreadyHappened)
    {
        mActiveFlagsContainer.AddCount(flagName, increase);
		onActiveFlagStateChanged(flagName, increase, impactAlreadyHappened);
    }

    /// <summary>
    /// Owner的动态标记发生变化, 通知每个Action, 从而得知这些Action哪些需要被终止.
    /// </summary>
    /// <param name="flagName">动态标记标识</param>
	/// <param name="increased">该标识当前的计数是增加还是减少</param>
	/// <param name="impactAlreadyHappened">影响已经发生的技能效果, 动作等</param>
	protected virtual void onActiveFlagStateChanged(ActiveFlagsDef flagName, bool increased, bool impactAlreadyHappened)
    {
		if (impactAlreadyHappened && mActionCenter != null)
			mActionCenter.OnActiveFlagsStateChanged(flagName, increased);

		uint count = mActiveFlagsContainer[flagName];

		switch (flagName)
		{
			// 如果`神圣`的标志值增加, 那么移除当前所有的有害技能效果.
			case ActiveFlagsDef.Inviolability:
				if (impactAlreadyHappened && mSkillEffectManager != null && increased)
					mSkillEffectManager.ForEverySkillEffect(
						new SkillUtilities.KillHarmfulSkillBuff(true)
						);
				break;
			// 描边效果.
			case ActiveFlagsDef.StrokeEffect:
				{
					if (count != 0) 
						FlyIntoaRage();
					else
						LeaveFromaRange();
				}
				break;
			// 变身恢复.
			case ActiveFlagsDef.Transformed:
				if (count == 0)
				{
					if (mModelResID != InitModelID)
						ChangeModel(InitModelID);

					ChangeSkill(null);
				}
				break;
			// 免疫控制, 移除当前单位上带有控制的技能效果.
			case ActiveFlagsDef.StunImmunity:
				if (impactAlreadyHappened && mSkillEffectManager != null && increased)
					mSkillEffectManager.ForEverySkillEffect(
						new SkillUtilities.KillSkillBuffWithStunEffect()
						);
				
				break;
		}
    }

    public void StopAllSkills()
    {
        if (mActionCenter != null)
            mActionCenter.RemoveActionByType(ActionTypeDef.ActionTypeSkill);
    }

    /// <summary>
    /// 检测并在当前角色应用随机事件.
    /// </summary>
    /// <param name="attackerAttr">触发该事件的角色的属性</param>
    /// <param name="skillCommonResID">触发该事件的技能ID</param>
    /// <param name="type">事件触发类型</param>
    public void ApplyRandEvent(BattleUnit theOther, RandEventTriggerType type, RandEventArg argument)
    {
		SkillDetails.ApplyRandEvent(theOther, this, mRandEventContainer, type, argument);
    }

    public float GetSkillCD(int skillID)
    {
        if (mSkillContainer == null)
            return 0.0f;
        BattleUnitSkill skill = mSkillContainer.GetSkill(skillID);
        if (skill == null)
            return 0.0f;
        return skill.CdMilliseconds;
    }

    //使用当前武器的默认技能
	public ErrorCode UseWeaponSkill()
	{
		int weaponSkillID = GetWeaponSkillID();
		if(weaponSkillID >= 0)
			return UseSkill(weaponSkillID, Vector3.zero);

		return ErrorCode.Succeeded;
	}

	/// <summary>
	/// <para>向targetPosition使用技能.</para>
	/// 如果targetPosition为zero, 那么会根据技能索敌等情况, 自动确定目标点.
	/// </summary>
    public ErrorCode UseSkill(int resID, Vector3 targetPosition)
    {
        if (mSkillContainer == null)
            return ErrorCode.InvalidParam;

        if (mScene == null || mScene.isSafeScene())
            return ErrorCode.NotBattleScene;

		BattleUnitSkill skill = mSkillContainer.GetSkill(resID);

		if (skill == null)
			return ErrorCode.InvalidParam;

		if (skill.IsRegularAttack)
		{
			// 使用武器技能时, 检查随机事件, 从而确定是否需要改变武器技能的ID.
			StartRegularSkillEventArg argument = new StartRegularSkillEventArg((uint)resID);
			ApplyRandEvent(null, RandEventTriggerType.OnStartRegularSkill, argument);
			if (resID != (int)argument.SkillResID
				&& (skill = mSkillContainer.GetSkill((int)argument.SkillResID)) == null)
				return ErrorCode.InvalidParam;
		}

		// 检查技能基本信息.
		ErrorCode err = SkillUtilities.CheckSkillGeneralInfo(this, skill);
		if (err != ErrorCode.Succeeded)
		{
			ErrorHandler.Parse(err, "failed to CheckSkillGeneralInfo");
			return err;
		}

		// 检查技能使用者的当前技能状态.
		if ((err = SkillUtilities.CheckUserSkillUsingState(mActionCenter, skill)) != ErrorCode.Succeeded)
		{
			ErrorHandler.Parse(err, "failed to CheckUserSkillUsingState");
			return err;
		}

		// 无效的目标点, 重新确定目标点.
		if (targetPosition == Vector3.zero
			&& (!skill.skillRes.autoAim || (targetPosition = GetAimTargetPos()) == Vector3.zero))
			targetPosition = Utility.MoveVector3Towards(GetPosition(), GetDirection(), 10f);

		return StartSkill(skill, targetPosition);
    }

	public virtual Vector3 GetAimTargetPos()
    {
        return Vector3.zero;
    }

	/// <summary>
	/// 显示/隐藏防护盾界面.
	/// </summary>
	public void ShowShieldUI(bool show)
	{
		if (mUIShield == null)
			mUIShield = new UIShield();

		mUIShield.Visible = show;
	}

	/// <summary>
	/// 设置防护盾界面的进度.
	/// </summary>
	public void SetShieldUIProgress(float value)
	{
		mUIShield.Progress = value;
	}

    public bool IsSkillUsing()
    {
        if (mActionCenter == null)
            return false;

        return mActionCenter.GetActionByType(ActionTypeDef.ActionTypeSkill) != null;
    }
 
    public bool CanRegularAttack()
    {
        if (!isAlive())
            return false;

		if (mScene == null)
			return false;

		if (!mScene.IsActionFlagAllowed(SceneActionFlag.SceneActionFlag_Attack))
			return false;

        return mActiveFlagsContainer[ActiveFlagsDef.DisableAttack] == 0;
    }

	public bool CanChangeWeapon()
	{
		return mActiveFlagsContainer[ActiveFlagsDef.DisableChangeWeapon] == 0;
	}

    public bool CanUseSkill()
    {
        if (!isAlive())
            return false;

        if (mScene == null)
            return false;

        if (!mScene.IsActionFlagAllowed(SceneActionFlag.SceneActionFlag_UseSkill))
            return false;
		
        return mActiveFlagsContainer[ActiveFlagsDef.DisableSkillUse] == 0;
    }

	/// <summary>
	/// 见ActiveFlagsDef.Inviolability.
	/// </summary>
	public bool IsInviolable() {
		return mActiveFlagsContainer[ActiveFlagsDef.Inviolability] != 0;
	}

	/// <summary>
	/// 能否被控制(控制的定义见SkillUtilities.IsStunFlag).
	/// </summary>
    public bool CanBeStuned()
    {
        return !IsInviolable() && mActiveFlagsContainer[ActiveFlagsDef.StunImmunity] == 0;
    }

	/// <summary>
	/// 能否被伤血.
	/// </summary>
	public bool CanBeDamaged()
	{
		return !IsInviolable() && mActiveFlagsContainer[ActiveFlagsDef.DamageImmunity] == 0;
	}

	/// <summary>
	/// 返回true, 表示免疫某些类型的技能效果.
	/// </summary>
	protected virtual bool SkillEffectImmunity(SkillEffectType type)
	{
		return false;
	}

	/// <summary>
	/// 添加出生技能效果, 不会检查是否可以添加该效果.
	/// </summary>
	/// <param name="summonerAttr">召唤者的属性, 如果没有召唤者, 那么通过this构造该结构</param>
	protected ErrorCode AddBornSkillEffect(AttackerAttr summonerAttr, SkillEffectType type, uint resID)
	{
		ErrorCode err = ErrorCode.ConfigError;
		switch (type)
		{
			case SkillEffectType.Displacement:
				err = SkillDetails.StartDisplace(mActionCenter, summonerAttr, resID);
				break;
			case SkillEffectType.Buff:
			case SkillEffectType.Impact:
				err = SkillDetails.StartSkillEffect(summonerAttr, mSkillEffectManager, type, resID);
				break;
			default:
				break;
		}

		return err;
	}

    public bool IsDead()
    {
        return mActiveFlagsContainer[ActiveFlagsDef.IsDead] != 0;
    }

    public bool isAlive()
    {
        return !IsDead();
    }

    /// <summary>
    /// 进入战斗状态.
    /// </summary>
    public void EnterFightState()
    {
        IdleIndex = IdleStateDef.Fight;
        if (mActionCenter != null)
            mActionCenter.EnterFightState();
    }

    public bool IsFighting()
    {
        if (mActionCenter != null)
            return mActionCenter.IsFighting();

        return false;
    }

    public override void RecieveMecAnimEvent(MecanimEvent eve)
    {
        //if (eve != null && eve.context.layer == (int)AnimationLayer.BaseLayer && eve.name == MecanimEvent.MEC_ANIM_END)
        //{
        //    if (mActionCenter != null)
        //    {
        //        ActionIdle idleAction = mActionCenter.GetActionByType(typeof(ActionIdle)) as ActionIdle;
        //        if (idleAction != null)
        //        {
        //            idleAction.SetActive(true);
        //        }
        //    }
        //}
    }

    public bool Die(AttackerAttr killerAttr, ImpactDamageType impactDamageType = ImpactDamageType.Invalid)
    {
        if (!IsDead())
        {
            AddActiveFlag(ActiveFlagsDef.IsDead, true, true);

            onDie(killerAttr, impactDamageType);

            if (mScene != null)
            {
                mScene.OnKillEnemy(this, killerAttr.AttackerID);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 战斗过程中, 给battleunit添加伤害(伤害值可正可负, 负值表示增血).
    /// </summary>
    /// <returns>单位是否依然活着.</returns>
    public bool AddDamage(DamageInfo damage, AttackerAttr attackerAttr)
    {
		if (GetPropertyValue((int)PropertyTypeEnum.PropertyTypeHP) + damage.Value <= 0)
		{
			FatalStrikeEventArg fatalStrikeArg = new FatalStrikeEventArg();

			// 致命一击随机事件.
			ApplyRandEvent(null, RandEventTriggerType.OnFatalStrike, fatalStrikeArg);

			if (fatalStrikeArg.BlockThisDamage)
				return true;
		}

		ModifyPropertyValue((int)PropertyTypeEnum.PropertyTypeHP, damage.Value);

		if (GetHP() <= 0)
		{
			Die(attackerAttr, damage.DamageType);
			//return false;
		}

        onDamage(damage, attackerAttr);
		//return true;
		return isAlive();
    }

    /// <summary>
    /// 战斗过程中, 改变battleunit的魔法.
    /// </summary>
    /// <param name="mana">魔法改变值, 正值表示加魔法, 否则为减魔法.</param>
    public void AddMana(int mana)
    {
        ModifyPropertyValue((int)PropertyTypeEnum.PropertyTypeMana, mana);
		onManaChanged(mana);
    }

	protected virtual void onManaChanged(int delta)
	{ 
		
	}

	protected virtual void onSkillBuffAdded(uint resID)
	{
		SkillBuffTableItem buffRes = DataManager.BuffTable[resID] as SkillBuffTableItem;
		Vector3 headPos = Vector3.zero;
		if (!string.IsNullOrEmpty(buffRes.icon) && (headPos = this.GetBonePositionByName("head")) != Vector3.zero)
		{
			headPos = CameraController.Instance.WorldToScreenPoint(headPos);
			headPos.z = 0.0f;

			BattleUIEvent evt = new BattleUIEvent(BattleUIEvent.BATTLE_UI_BUFF_ADDED);
			evt.bmpPath = buffRes.icon;
			evt.pos = headPos;
			EventSystem.Instance.PushEvent(evt);
		}
	}

	/// <summary>
	/// 获取召唤者的信息.
	/// </summary>
	public virtual uint SummonerID
	{
		get { return uint.MaxValue; }
	}

	public virtual string SummonerName { get { return string.Empty; } }

	public virtual uint SummonerLevel
	{
		get { return 1; }
	}

    /// <summary>
    /// 单位受到来自attackerAttr的damage伤害.
    /// </summary>
    protected virtual void onDamage(DamageInfo damage, AttackerAttr attackerAttr)
    {
		if (damage.Value < 0)
		{
			if (mBattleUintAI != null)
			{
				mBattleUintAI.OnBeHit(attackerAttr.AttackerID, Mathf.Abs(damage.Value));
			}

			//挨打闪烁
			StartTwinkle();

			if (kCriticalStrikeEffectID != uint.MaxValue && damage.Critical)
			{
				if (isAlive())
					AddEffect(kCriticalStrikeEffectID, GameConfig.CriticalStrikeEffectBindPoint);
				// 暴击死亡特效不显示.
				//else
				//	Scene.CreateEffect((int)kCriticalStrikeEffectID, Vector3.one, GetPosition(), GetDirection());
			}
		}

		Scene.onDamage(this, damage, attackerAttr);
    }

    public virtual int GetDeadSound()
    {
        return -1;
    }

	/// <summary>
	/// 获取死亡动作.
	/// </summary>
	public virtual string GetDeathAnimation()
	{
		// 随机选取一个.
		return mStateController.AnimSet.GetDie(UnityEngine.Random.Range(0, mStateController.AnimSet.NumOfDie));
	}

    public virtual int GetWalkSound()
    {
        return -1;
    }

    public virtual int GetCrySound()
    {
        return -1;
    }

    /// <summary>
    /// 添加材质特效和声音.
    /// </summary>
    public ErrorCode AddMaterialBehaviour(MaterialBehaviourDef name, ImpactDamageType impactDamageType, float dir)
    {
		// 无效的伤害类型.
		if (impactDamageType >= ImpactDamageType.Count)
			return ErrorCode.Succeeded;

        if (name != MaterialBehaviourDef.OnMaterialDie && IsDead())
            return ErrorCode.TargetIsDead;

        if( name == MaterialBehaviourDef.OnMaterialDie )
        {
            uint materialID = GetMaterialResourceID();

            if (materialID == uint.MaxValue)
            {
                ErrorHandler.Parse(ErrorCode.ConfigError, dbgGetIdentifier() + " 没有材质!");
                return ErrorCode.ConfigError;
            }

            MaterialTableItem material = DataManager.MaterialTable[materialID] as MaterialTableItem;
            if (material == null)
            {
                SkillUtilities.ResourceNotFound("material", materialID);
                return ErrorCode.ConfigError;
            }

            if (material.deathEffectWaitTime > 0)
            {
                Scene.GetDelayBehaviourManager().AddDelayBehaviour(this, name, impactDamageType, dir,material.deathEffectWaitTime);
                return ErrorCode.Succeeded;
            }
        }

        return SkillDetails.AddMaterialBehaviour(this, mHitMaterialEffectCdContainer, name, impactDamageType, dir);
    }

    public HitMaterialEffectCdContainer GetHitMaterialEffectCdContainer()
    {
        return mHitMaterialEffectCdContainer;
    }

    protected virtual void onDie(AttackerAttr killerAttr, ImpactDamageType impactDamageType)
    {

        MaterialTableItem material = null;
        uint materialID = GetMaterialResourceID();

        if (materialID != uint.MaxValue)
        {
            material = DataManager.MaterialTable[materialID] as MaterialTableItem;
        }
        bool operate = false;
        //根据材质修正尸体的存在时间
        if (impactDamageType < ImpactDamageType.Count)
        {
            if (material != null)
            {
				MaterialItem item = material.items[(int)impactDamageType];
				if (item.deatdelay >= 0)
                {
					mWaitTime = item.deatdelay;
                    mDisappearTime = 0;
                    operate = true;

                }
            }
        }

        if(!operate)
        {
            mWaitTime = mMaxWaitDisappearTime;
            mDisappearTime = mMaxDisappearTime;
        }

        // 检查击杀的随机事件.
        BattleUnit killer = killerAttr.CheckedAttackerObject();
        // 检查是否是自杀.
        if (killer != null && InstanceID != killerAttr.AttackerID)
        {
            killer.OnKillOther(killerAttr, this);
        }

		// 系统回收不触发死亡事件, 自杀依然触发.
		// 如果凶手通过随机事件杀死this, 那么不触发this的随机事件.
		if (killerAttr.AttackerID != uint.MaxValue && !killerAttr.StructMadeByRandEvent)
			ApplyRandEvent(killer, RandEventTriggerType.OnBeKilled, new OnBeKilledEventArg(killerAttr));

        // 通知skilleffect的清理.
        SkillDetails.OnSkillEffectOwnerEvent(mSkillEffectManager, SkillEffectOwnerEventDef.OwnerDie);

        if (material != null && impactDamageType < ImpactDamageType.Count)
        {
			MaterialItem item = material.items[(int)impactDamageType];
			if (item.mtl)
            {
                ////火死亡
                if (impactDamageType == ImpactDamageType.Fire)
                {
                    SetDeathMaterial("burn_out");
                }
                ////毒死亡
                if (impactDamageType == ImpactDamageType.Poison)
                {
                    SetDeathMaterial("poison");
                }
                //冰死亡
                if (impactDamageType == ImpactDamageType.Frost)
                {
                    SetDeathMaterial("iceimpact");
                }
            }
        }

        // 死亡特效, 声音, 动作等.
		// 当所有的效果, 动作都结束时, 开始死亡动作.
        if (mActionCenter != null)
            ErrorHandler.Parse(
                mActionCenter.StartAction(new ActionDieInitParam() { attackerAttr = killerAttr, damageType = impactDamageType }),
                "failed to create action die"
                );

        DieAward(killerAttr.AttackerID);
    }

    /// <summary>
    /// 当本单位击杀他人时调用.
    /// </summary>
	/// <param name="myAttrWhenKill">表示该battleunit在击杀技能发起时的属性.</param>
    protected virtual void OnKillOther(AttackerAttr myAttrWhenKill, BattleUnit theDead)
	{
		// 如果this, 通过随机事件击杀, 那么不触发击杀随机事件.
		if (!myAttrWhenKill.StructMadeByRandEvent)
			ApplyRandEvent(theDead, RandEventTriggerType.OnKillOthers, null);
    }

    /// <summary>
    /// 获取材质资源ID(material.txt), 在受到伤害时用到.
    /// </summary>
    /// <returns></returns>
    public virtual uint GetMaterialResourceID()
    {
        return uint.MaxValue;
    }

    public LeagueDef GetLeague() { return mLeague; }

    public virtual uint GetLevel() { return 1; }

    public virtual string GetName()
    {
        return "";
    }

    public void SetLeague(LeagueDef newLeague)
    {
        mLeague = newLeague;
    }

    virtual public int GetMainWeaponID()
    {
        return -1;
    }

	virtual public bool ChangeWeapon(int weaponid)
	{
		return false;
	}
	/// <summary>
	/// 替换非武器技能.
	/// </summary>
	virtual public bool ChangeSkill(int[] skillContainer)
	{
		if (skillContainer != null)
			GameDebug.LogError("无法为" + dbgGetIdentifier() + "替换技能");
		return false;
	}

    //获取当前武器的总弹药数量
    virtual public int GetWeaponMaxBullet()
    {
        return -1;
    }
    //获取当前武器 剩余弹药
    virtual public int GetWeaponBullet()
    {
        return 0;
    }
    //消耗弹药
    virtual public void CostWeaponBullet(int cost)
    {

    }

    virtual public void AddWeaponBullet(int cost)
    {

    }

    protected void ReloadBullet()
    {
        ActionReloadInitParam reloadParam = new ActionReloadInitParam();
        reloadParam.weaponid = GetMainWeaponID();
        mActionCenter.StartAction(reloadParam);
    }

    //获取当前主武器的技能ID
    virtual public int GetWeaponSkillID()
    {
        return -1;
    }

    public override bool UpdateDestroy(uint elapsed)
    {
        UpdateMecanim(elapsed);

        if (mActionCenter != null)
            mActionCenter.UpdateActions(elapsed);


        UpdateTwinkle(elapsed);

        if (mWaitTime <= 0.0f && mDisappearTime <= 0.0f)
        {
            return false;
        }

        if (mWaitTime >= 0.0f)
        {
            mWaitTime -= elapsed;
        }
        else
        {
            mDisappearTime -= elapsed;
        }

        

        base.UpdateDestroy(elapsed);
        return true;
    }
    public override bool Update(uint elapsed)
    {
        if (mActionCenter != null)
            mActionCenter.UpdateActions(elapsed);

        if (mSkillEffectManager != null)
            mSkillEffectManager.UpdateSkillEffects(elapsed);

        if (mRandEventContainer != null)
            mRandEventContainer.Update(elapsed);

        //临时...模型没创建出来 AI不走.. (因为AI会给角色加特效 特效加不上)
        if (mBattleUintAI != null && mVisual != null && mVisual.Visual != null && mScene.IsActionFlagAllowed(SceneActionFlag.SceneActionFlag_Ai))
        {
            if (!IsBorning())
            {
               mBattleUintAI.Update(elapsed);
            }
        }

        if (mHitMaterialEffectCdContainer != null)
            mHitMaterialEffectCdContainer.Update(elapsed);

		if (mSpasticityCd >= elapsed)
			mSpasticityCd -= elapsed;
		else
			mSpasticityCd = 0;

        UpdateTwinkle(elapsed);

		updateShieldUIPosition();

        return base.Update(elapsed);
    }

	/// <summary>
	/// 更新防护盾界面的位置.
	/// </summary>
	void updateShieldUIPosition()
	{
		if (mUIShield != null && mUIShield.Visible)
		{
			Vector3 headPos = this.GetBonePositionByName("head");
			if (headPos != Vector3.zero)
			{
				headPos = CameraController.Instance.WorldToScreenPoint(headPos);
				headPos.Set(headPos.x, headPos.y + 12, 0f);
				mUIShield.Position = headPos;
			}
		}
	}

    protected void UpdateTwinkle(uint elapsed)
    {
        if (mTwinkleTime > 0.0f)
        {
            float twinklePercent = mTwinkleTime / twinkeLast;
            mTwinkleTime -= elapsed;

            Color nowColor = Color.white;
            nowColor.g = nowColor.b = iTween.easeInOutBack(1, 0, twinklePercent);

            if (mVisual != null && mVisual.GetRenderer() != null)
            {
                Renderer renderer = mVisual.GetRenderer();
                foreach (Material mtl in renderer.materials)
                {
                    mtl.SetColor("_Color", mTwinkleTime > 0.0f ? nowColor : Color.white);
                }
            }

        }

    }
    //正在出生前
    virtual protected bool IsBorning()
    {
        return false;
    }

    //对外接口 增加某个ID 属性 ID 对应propery.xml
    public void ModifyPropertyValue(int id, int value)
    {
        if( id == (int)PropertyTypeEnum.PropertyTypeHP ||
            id == (int)PropertyTypeEnum.PropertyTypeMana)
        {
            if (IsWudi())
                return;
        }

        int Value = value;
        if (id == (int)PropertyTypeEnum.PropertyTypeHP)
        {
            if (this.GetHP() + Value > this.GetMaxHP())
            {
                Value = this.GetMaxHP() - this.GetHP();
            }
        }
        else if (id == (int)PropertyTypeEnum.PropertyTypeMana)
        {
            if (this.GetMana() + Value > this.GetMaxMana())
            {
                Value = this.GetMaxMana() - this.GetMana();
            }
        }

        float oldValue = mProperty.GetProperty(id);
        mProperty.AddProperty(id, (float)Value);
        onPropertyChanged(id, oldValue, mProperty.GetProperty(id));
    }

    public int GetPropertyValue(int id)
    {
        return (int)mProperty.GetProperty(id);
    }

    protected void SetBaseProperty(int id, int value)
    {
        float oldValue = mProperty.GetProperty(id);
        mProperty.SetBaseProperty(id, value);
    }
    
	protected void SetBaseProperty(int id, float value)
    {
        float oldValue = mProperty.GetProperty(id);
        mProperty.SetBaseProperty(id, value);
    }

    protected virtual void onPropertyChanged(int id, float oldValue, float newValue)
    {
        switch ((PropertyTypeEnum)id)
        {
			case PropertyTypeEnum.PropertyTypeAlpha:
				ChangeAlpha(newValue / 100f);
				break;
			case PropertyTypeEnum.PropertyTypeScale_Rate:
				if (mVisual != null && mVisual.Visual != null)
				{
					if (Utility.isZero(newValue) || Utility.isZero(oldValue))
						GameDebug.LogError("模型缩放不可为0");
					else
                        mVisual.VisualTransform.localScale *= (newValue / oldValue);
				}
				break;
            default:
                break;
        }
    }

    // 	public void SetHP(int hp)
    // 	{
    // 		int curHP = GetHP();
    // 		int maxHp = GetMaxHP();
    // 
    // 		if (hp < 0)
    // 			hp = 0;
    // 		else if (hp > maxHp)
    // 			hp = maxHp;
    // 		
    // 		if (hp == curHP)
    // 			return;
    // 		
    // 		SetBaseProperty( (int)PropertyTypeEnum.PropertyTypeHP , hp );
    // 	}
    // 
    // 	public void SetMana(int mana)
    // 	{
    // 		int curMana = GetMana();
    // 		int maxMana = GetMaxMana();
    // 		
    // 		if (mana < 0)
    // 			mana = 0;
    // 		else if (mana > maxMana)
    // 			mana = maxMana;
    // 		
    // 		if (mana == curMana)
    // 			return;
    // 		
    // 		SetBaseProperty( (int)PropertyTypeEnum.PropertyTypeMana , mana );
    // 	}

    public float GetSpeed()
    {
        return mProperty.GetProperty((int)PropertyTypeEnum.PropertyTypeSpeed);
    }

    public int GetHP()
    {
        return (int)mProperty.GetProperty((int)PropertyTypeEnum.PropertyTypeHP);
    }

    public int GetMaxHP()
    {
        return (int)mProperty.GetProperty((int)PropertyTypeEnum.PropertyTypeMaxHP);
    }

    public int GetMana()
    {
        return (int)mProperty.GetProperty((int)PropertyTypeEnum.PropertyTypeMana);
    }

    public int GetMaxMana()
    {
        return (int)mProperty.GetProperty((int)PropertyTypeEnum.PropertyTypeMaxMana);
    }

    //朝着方向移动
    virtual public bool MoveDir(float dir)
    {
        if (mScene == null)
            return false;

        if (mActionCenter == null)
            return false;
        PlayerController.Instance.BreakQuestMove();
        ActionMoveInitParam movParam = new ActionMoveInitParam();
        movParam.wayOrDir = false;
        movParam.dir = dir;
        return mActionCenter.StartAction(movParam) == ErrorCode.Succeeded;
    }

    //移动到一个固定点
    virtual public void MovePos(Vector3 pos)
    {
        MoveTo(new Vector3f(pos.x, pos.y, pos.z));
    }

    public virtual bool MoveTo(Vector3f pos)
    {
        if (mScene == null)
            return false;

        if (mActionCenter == null)
            return false;

        Vector3f curPos = GetPosition3f();
        List<Vector2f> wayPath = mScene.FindPath(this, new Vector2f(curPos.x, curPos.z), new Vector2f(pos.x, pos.z));
        if (wayPath == null)
            return false;

        ActionMoveInitParam movParam = new ActionMoveInitParam();
        for (int i = 0; i < wayPath.Count; i++)
        {
            Vector2f v = wayPath[i];
            movParam.wayPoint.Add(new Vector3f(v.x, 0.0f, v.y));
        }
        return mActionCenter.StartAction(movParam) == ErrorCode.Succeeded;
    }

    //停止所有移动
    virtual public void StopMove()
    {
       
        if (mActionCenter == null)
            return;
		mActionCenter.RemoveActionByType(ActionTypeDef.ActionTypeMove);
    }

    //停止方向移动
    virtual public void StopDirMove()
    {
        if (mActionCenter == null)
            return;

        ActionMove am = mActionCenter.GetActionByType(ActionTypeDef.ActionTypeMove) as ActionMove;

        if( am != null && am.IsDirection() )
        {
            mActionCenter.RemoveActionByType(ActionTypeDef.ActionTypeMove);
        }
    }

	public bool HasMagneticEffect()
	{
		return mActiveFlagsContainer[ActiveFlagsDef.MagneticEffect] != 0;
	}

    //是否可以移动
    virtual public bool IsCanMove()
    {
		if(!isAlive())
		{
			return false;
		}

		if(mActiveFlagsContainer[ActiveFlagsDef.DisableMovement] != 0)
		{
			return false;
		}

		if(mScene == null)
		{
			return false;
		}

		if(!mScene.IsActionFlagAllowed(SceneActionFlag.SceneActionFlag_Move))
		{
			return false;
		}

		return true;
    }

    public bool IsCanRotation()
    {
        return mActiveFlagsContainer[ActiveFlagsDef.DisableRotate] == 0;
    }
    //是否正在移动
    virtual public bool IsMoveing()
    {
        if (mActionCenter == null)
            return false;

        return mActionCenter.GetActionByType(ActionTypeDef.ActionTypeMove) != null;
    }
    //是否能移动改变方向
    virtual public bool IsCanMoveRotation()
    {
        return true;
    }

    // 取得移动的目标位置
    public Vector3f GetMoveTargetPos()
    {
        if (mActionCenter == null)
            return new Vector3f();

        Action action = mActionCenter.GetActionByType(ActionTypeDef.ActionTypeMove);
        if (action == null)
            return new Vector3f();

        ActionMove moveAction = (ActionMove)action;
        return moveAction.GetMoveTargetPos();
    }

    public void TurnTo(Vector3 tarPos)
    {
        SetDirection(Utility.Angle2D(tarPos, GetPosition()) * UnityEngine.Mathf.Deg2Rad);
    }

    public void TurnTo(Vector3f tarPos)
    {
        TurnTo(new Vector3(tarPos.x, tarPos.y, tarPos.z));
    }

    public void Turn(float angle)
    {
        float v = GetDirection() + angle * UnityEngine.Mathf.Deg2Rad;
        SetDirection(v);
    }

    public Vector3f GetHomePosition()
    {
        return mHomePosition;
    }


    public float GetMoveDirection()
    {
        return mMoveDirection;
    }

    public void SetMoveDirection(float dir)
    {
        mMoveDirection = dir;
    }

    public bool IsGMFly()
    {
        return false;
    }

    virtual protected void DieAward(uint killerid)
    {

    }


    virtual public void HpDamageAward(uint objtarget,int time)
    {

    }

    virtual public bool IsGuildTarget()
    {
        return false;
    }

    virtual public void Relive(int hp, int mp)
    {

        RemoveDeathMaterial();

        mActionCenter.RemoveActionByType(ActionTypeDef.ActionTypeDie);
        AddActiveFlag(ActiveFlagsDef.IsDead, false, true);

        ModifyPropertyValue((int)PropertyTypeEnum.PropertyTypeHP, hp);
        ModifyPropertyValue((int)(int)PropertyTypeEnum.PropertyTypeMana, mp);
    }

    public void ResetAllSKillCd()
    {
        mSkillContainer.ResetAllSkillCD();
    }

	public void ResetSkillCD(int resID)
	{
		mSkillContainer.ResetSkillCD(resID);
	}

    /// <summary>
    /// 挨打闪烁
    /// </summary>
    public void StartTwinkle()
    {
        mTwinkleTime = twinkeLast;

    }

    protected virtual void OnEnterFury()
    {

    }

    protected virtual void OnLeaveFury()
    {

    }

    public virtual bool IsFury()
    {
        return mMtlFlags.Get((int)MtlFlag.Rage);
    }

    /// <summary>
    /// 进入狂暴
    /// </summary>
    public void FlyIntoaRage()
    {
        if (mMtlFlags.Get((int)MtlFlag.Rage))
            return;

        mRageTask = new RageMtlObjTask(this, "rimlight");
        AddVisualTask(mRageTask);

        mMtlFlags.Set((int)MtlFlag.Rage,true);

        OnEnterFury();
    }
    
    public void LeaveFromaRange()
    {
		if (mRageTask != null)
			mRageTask.Destroy();
        RemoveVisualTask(mRageTask);
        mMtlFlags.Set((int)MtlFlag.Rage,false);
        OnLeaveFury();
    }

    public void SetWudi(bool wudi)
    {
        mWudi = wudi;
    }

    public bool IsWudi()
    {
        return mWudi;
    }

    public virtual void PlayWeaponAnim(string statename)
    {

    }

    public virtual void PlayWingAnim(string statename)
    {

    }

}
