public class SkillCommonTableItem
{
	public int resID;
	public string desc;

	// 是否为普通攻击.
	public bool isRegularAttack;

    //是否支持自动瞄准敌人
    public bool autoAim;

	// 技能CD时间.
	public uint myCd;

	// 魔法消耗.
	public uint manaCost;

	// 子弹消耗.
	public uint bulletCost;

	// 技能在准备阶段时, 能否被使用者自身使用其它技能打断, 或者再次使用本技能, 通过二次使用技能而使技能进入使用阶段.
	public bool interruptable;

	// 技能准备时间, 从技能一开始运行就开始.
	public uint chargeTime;

	// 技能效果开始时间, 从技能执行状态(using)开始.
	public uint skillEffectStartTime;

	// 技能使用阶段剩余的循环次数.
	public uint skillUseStateLoopLeft;

	// 技能使用状态的维持时间.
	public uint skillUseStateDuration;

	// assert(skillUseStateDuration > effectStartTime);

	// 技能的最小/大范围.
	// 目标点距离玩家的距离只有在这个范围之内, 技能才可以释放.
	public float minRange;
	public float maxRange;

	// 准备和使用状态时的动作, 声音等(指向skillclientbehaviour.txt).
	public uint chargeBehaviour;
	public uint useBehaviour;

	// 技能一开始, 就给单位添加的buff, 直到技能结束时停止.
	public uint buffToSkillUser;

	// 技能产生效果时, 给技能使用者加的效果.
	public uint skillEffect2UserInUseState;

	// 对发起者前方distForward距离的一类单位产生效果或者创建召唤物.
	public float distForward;

	// 目标选择.
	public uint targetSelection;

	// 技能执行时效果, 指向skilleffect.txt
	public uint skillEffect2Others;

	// 投掷物资源ID.
	public uint projectileResID;

	// 创建召唤物(creationID).
	public uint creationID;
}
