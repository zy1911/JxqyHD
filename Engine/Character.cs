﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Engine.Gui;
using Engine.ListManager;
using Engine.Map;
using Engine.Script;
using IniParser;
using IniParser.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Engine
{
    using StateMapList = Dictionary<int, ResStateInfo>;

    public class FlyIniInfoItem : IEquatable<FlyIniInfoItem> , IComparable<FlyIniInfoItem>
    {
        public int UseDistance;
        public Magic TheMagic;

        public FlyIniInfoItem(int useDistance, Magic theMagic)
        {
            UseDistance = useDistance;
            TheMagic = theMagic;
        }

        public bool Equals(FlyIniInfoItem other)
        {
            if (other == null) return false;
            return UseDistance == other.UseDistance && TheMagic.FileName == other.TheMagic.FileName;
        }

        public int CompareTo(FlyIniInfoItem other)
        {
            if (other == null) return 1;
            if (UseDistance == other.UseDistance) return 0;
            if (UseDistance < other.UseDistance) return -1;
            return 1;
        }

        public void SetLevel(int attackLevel)
        {
            if (TheMagic != null)
            {
                TheMagic = TheMagic.GetLevel(attackLevel);
            }
        }
    }

    public abstract class Character : Sprite
    {
        #region Field
        private SoundEffectInstance _sound;
        private int _dir;
        private string _name;
        private int _kind;
        private int _relation;
        private int _pathFinder;
        private int _state;
        private int _visionRadius;
        private int _dialogRadius;
        private int _attackRadius;
        private int _lum;
        private int _action;
        private int _walkSpeed = 1;
        private int _evade;
        private int _attack;
        private int _attack2;
        private int _attackLevel;
        private int _defend;
        private int _defend2;
        private int _exp;
        private int _levelUpExp;
        private int _level;
        private int _life;
        private int _lifeMax;
        private int _thew;
        private int _thewMax;
        private int _mana;
        private int _manaMax;
        private StateMapList _npcIni = new StateMapList();
        private string _npcIniFileName;
        private Obj _bodyIni;
        private Magic _flyIni;
        private Magic _flyIni2;
        private List<FlyIniInfoItem> _flyIniInfos = new List<FlyIniInfoItem>();
        private string _flyInis;
        private Magic _magicToUseWhenLifeLow;
        private int _keepRadiusWhenLifeLow;
        private const int LifeLowPercentDefault = 20;
        private int _lifeLowPercent = LifeLowPercentDefault;
        private int _keepRadiusWhenFriendDeath;
        private Magic _magicToUseWhenBeAttacked;
        private int _magicDirectionWhenBeAttacked;
        private Magic _magicToUseWhenAttack;
        private Magic _magicToUseWhenDeath;
        private int _magicDirectionWhenDeath;
        private string _scriptFile;
        private string _deathScript;
        private string _timerScriptFile;
        private int _timerScriptInterval = Globals.DefaultNpcObjTimeScriptInterval;
        private float _timerScriptIntervlElapsed;
        private ScriptRunner _currentRunInteractScript;
        private ScriptRunner _currentRunDeathScript;
        private int _expBonus;
        private string _fixedPos;
        private int _idle;
        private Vector2 _magicDestination;
        private Character _magicTarget;
        private Vector2 _attackDestination;
        private bool _isInFighting;
        private float _totalNonFightingSeconds;
        private const float MaxNonFightSeconds = 7f;
        private Vector2 _destinationMovePositionInWorld = Vector2.Zero;
        private Vector2 _destinationMoveTilePosition = Vector2.Zero;
        private Vector2 _destinationAttackTilePosition = Vector2.Zero;
        private Vector2 _destinationAttackPositionInWorld = Vector2.Zero;
        private LinkedList<Vector2> _path;
        private object _interactiveTarget;
        private bool _isInInteract;
        private bool _isRunToTarget;
        private bool _isDeath;
        private float _poisonedMilliSeconds;
        private float _frozenSeconds;
        private bool _isFronzenVisualEffect;
        private float _poisonSeconds;
        private bool _isPoisionVisualEffect;
        private float _petrifiedSeconds;
        private bool _isPetrifiedVisualEffect;
        private bool _notAddBody;
        private bool _isNextStepStand;
        private Dictionary<int, Utils.LevelDetail> _levelIni;
        private bool _isInStepMove;
        private int _stepMoveDirection;
        private int _leftStepToMove;
        private int _directionBeforInteract;
        private int _specialActionLastDirection; //Direction before play special action
        private float _fixedPathDistanceToMove;
        private Vector2 _fixedPathMoveDestinationPixelPostion = Vector2.Zero;
        private readonly LinkedList<Npc> _summonedNpcs = new LinkedList<Npc>();
        private int _rangeRadiusToShow;

        /// <summary>
        /// List of the fixed path tile position.
        /// When load <see cref="FixedPos"/>, <see cref="FixedPos"/> is converted to list and stored on this value.
        /// </summary>
        protected List<Vector2> FixedPathTilePositionList;
        protected int _currentFixedPosIndex;
        protected Magic MagicUse;
        protected MagicSprite _controledMagicSprite;

        protected String _dropIni;

        protected bool IsInLoopWalk;

        protected bool IsSitted;

        #endregion Field

        #region Protected properties
        protected virtual bool IsMagicFromCache { get { return true; } }
        protected string LevelIniFile { set; get; }

        /// <summary>
        /// If true regenerate path when character move along path.
        /// </summary>
        protected bool MoveTargetChanged { set; get; }
        #endregion Protected properties

        #region Public properties

        public LinkedList<MagicSprite> MagicSpritesInEffect = new LinkedList<MagicSprite>();

        public Dictionary<int, Utils.LevelDetail> LevelIni
        {
            get { return _levelIni; }
            set { _levelIni = value; }
        }

        public MagicSprite ControledMagicSprite
        {
            get { return _controledMagicSprite; }
            set { _controledMagicSprite = value; }
        }

        public string DropIni
        {
            get { return _dropIni; }
            set { _dropIni = value; }
        }

        public bool IsFightDisabled { protected set; get; }
        public bool IsJumpDisabled { protected set; get; }
        public bool IsRunDisabled { protected set; get; }
        public Character FollowTarget { protected set; get; }
        public bool IsFollowTargetFound { protected set; get; }
        public bool IsInSpecialAction { protected set; get; }

        //Summon begin
        public MagicSprite SummonedByMagicSprite { set; get; }
        public int SummonedNpcsCount { get { return _summonedNpcs.Count; } }

        public void AddSummonedNpc(Npc npc)
        {
            if (npc == null)
            {
                return;
            }
            _summonedNpcs.AddLast(npc);
        }

        public void RemoveFirstSummonedNpc()
        {
            var node = _summonedNpcs.First;
            if (node != null)
            {
                var npc = node.Value;
                npc.Death();
            }
            _summonedNpcs.RemoveFirst();
        }
        //Summon end

        public bool IsInFighting
        {
            get { return _isInFighting; }
            protected set { _isInFighting = value; }
        }

        public bool IsDeathScriptEnd
        {
            get
            {
                if (_currentRunDeathScript == null)
                {
                    return true;
                }
                return _currentRunDeathScript.IsEnd;
            }
        }

        public bool IsNodAddBody
        {
            get { return _notAddBody; }
        }

        public bool IsHide { get; set; }

        /// <summary>
        /// If true, character won't be drawed.
        /// </summary>
        public bool IsInTransport { get; set; }

        public MagicSprite MovedByMagicSprite { set; get; }


        public float BouncedVelocity { set; get; }
        private Vector2 _bouncedDirection;

        public Vector2 BouncedDirection
        {
            get { return _bouncedDirection; }
            set
            {
                if (value != Vector2.Zero)
                {
                    value.Normalize();
                }
                _bouncedDirection = value;
            }
        }

        public bool IsInStepMove
        {
            get { return _isInStepMove; }
        }

        public float FrozenSeconds
        {
            get { return _frozenSeconds; }
            protected set
            {
                if (IsInDeathing || IsPetrified) return;
                if (value < 0) value = 0;
                _frozenSeconds = value;
            }
        }

        public float PoisonSeconds
        {
            get { return _poisonSeconds; }
            protected set
            {
                if (IsInDeathing || IsPetrified) return;
                if (value < 0) value = 0;
                _poisonSeconds = value;
            }
        }

        public float PetrifiedSeconds
        {
            get { return _petrifiedSeconds; }
            protected set
            {
                if (IsInDeathing) return;
                if (value < 0) value = 0;
                _frozenSeconds = 0;
                _petrifiedSeconds = value;
            }
        }

        public bool IsFrozened { get { return FrozenSeconds > 0; } }
        public bool IsPoisoned { get { return PoisonSeconds > 0; } }
        public bool IsPetrified { get { return PetrifiedSeconds > 0; } }

        public void SetFrozenSeconds(float s, bool hasVisualEffect)
        {
            if(_frozenSeconds > 0) return;
            FrozenSeconds = s;
            _isFronzenVisualEffect = hasVisualEffect;
        }

        public void SetPoisonSeconds(float s, bool hasVisualEffect)
        {
            if(_poisonSeconds > 0) return;
            PoisonSeconds = s;
            _isPoisionVisualEffect = hasVisualEffect;
        }

        public void SetPetrifySeconds(float s, bool hasVisualEffect)
        {
            if(_petrifiedSeconds > 0) return;
            PetrifiedSeconds = s;
            _isPetrifiedVisualEffect = hasVisualEffect;
        }

        public bool BodyFunctionWell
        {
            get
            {
                return (FrozenSeconds <= 0 &&
                        PoisonSeconds <= 0 &&
                        PetrifiedSeconds <= 0);
            }
        }

        public int Dir
        {
            get { return _dir; }
            set { _dir = value % 8; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int Kind
        {
            get { return _kind; }
            set { _kind = value; }
        }

        public int Relation
        {
            get
            {
                if (_controledMagicSprite != null)
                {
                    if (_relation == (int)RelationType.Enemy)
                    {
                        return (int)RelationType.Friend;
                    }
                }
                return _relation;
            }
            set { _relation = value; }
        }

        public int State
        {
            get { return _state; }
            set { _state = value; }
        }

        public StateMapList NpcIni
        {
            get { return _npcIni; }
            protected set { _npcIni = value; }
        }

        public virtual int PathFinder
        {
            get { return _pathFinder; }
            set { _pathFinder = value; }
        }

        public abstract Engine.PathFinder.PathType PathType { get; }

        public int VisionRadius
        {
            get { return _visionRadius == 0 ? 9 : _visionRadius; }
            set { _visionRadius = value; }
        }

        public int DialogRadius
        {
            get { return _dialogRadius == 0 ? 1 : _dialogRadius; }
            set { _dialogRadius = value; }
        }

        public int AttackRadius
        {
            get { return _attackRadius == 0 ? 1 : _attackRadius; }
            set { _attackRadius = value; }
        }

        public int Lum
        {
            get { return _lum; }
            set { _lum = value; }
        }

        public int Action
        {
            get { return _action; }
            set { _action = value; }
        }

        public int WalkSpeed
        {
            get { return _walkSpeed; }
            set { _walkSpeed = value < 1 ? 1 : value; }
        }

        public int Evade
        {
            get { return _evade; }
            set { _evade = value; }
        }

        public int Attack
        {
            get { return _attack; }
            set { _attack = value; }
        }

        public int Attack2
        {
            get { return _attack2; }
            set { _attack2 = value; }
        }

        public int AttackLevel
        {
            get { return _attackLevel; }
            set
            {
                _attackLevel = value;
                if (FlyIni != null)
                {
                    FlyIni = FlyIni.GetLevel(value);
                }
                if (FlyIni2 != null)
                {
                    FlyIni2 = FlyIni2.GetLevel(value);
                }
            }
        }

        public int Defend
        {
            get { return _defend; }
            set { _defend = value; }
        }

        public int Defend2
        {
            get { return _defend2; }
            set { _defend2 = value; }
        }

        public int Exp
        {
            get { return _exp; }
            set { _exp = value; }
        }

        public int LevelUpExp
        {
            get { return _levelUpExp; }
            set { _levelUpExp = value; }
        }

        public int Life
        {
            get { return _life; }
            set
            {
                _life = value < 0 ? 0 : value;
                if (_life > _lifeMax) _life = _lifeMax;
            }
        }

        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public int LifeMax
        {
            get { return _lifeMax; }
            set
            {
                _lifeMax = value;
                if (_life > value) _life = value;
            }
        }

        public int Thew
        {
            get { return _thew; }
            set
            {
                _thew = value < 0 ? 0 : value;
                if (_thew > _thewMax) _thew = _thewMax;
            }
        }

        public int ThewMax
        {
            get { return _thewMax; }
            set
            {
                _thewMax = value;
                if (_thew > value) _thew = value;
            }
        }

        public int Mana
        {
            get { return _mana; }
            set
            {
                _mana = value < 0 ? 0 : value;
                if (_mana > _manaMax) _mana = _manaMax;
            }
        }

        public int ManaMax
        {
            get { return _manaMax; }
            set
            {
                _manaMax = value;
                if (_mana > value) _mana = value;
            }
        }

        public Obj BodyIni
        {
            get { return _bodyIni; }
            set { _bodyIni = value; }
        }

        public bool IsBodyIniOk
        {
            get
            {
                return (BodyIni != null &&
                        BodyIni.ObjFile != null &&
                        BodyIni.ObjFile.Count > 0);
            }
        }

        public virtual Magic FlyIni
        {
            get { return _flyIni; }
            set
            {
                RemoveMagicFromInfos(_flyIni, AttackRadius);
                _flyIni = value;
                if (_flyIni != null) _flyIni = _flyIni.GetLevel(AttackLevel);
                AddMagicToInfos(_flyIni, AttackRadius);
            }
        }

        public virtual Magic FlyIni2
        {
            get { return _flyIni2; }
            set
            {
                RemoveMagicFromInfos(_flyIni2, AttackRadius);
                _flyIni2 = value;
                if (_flyIni2 != null) _flyIni2 = _flyIni2.GetLevel(AttackLevel);
                AddMagicToInfos(_flyIni2, AttackRadius);
            }
        }

        public virtual string FlyInis
        {
            get { return _flyInis; }
            set
            {
                _flyInis = value;
                var strs = value.Split(new []{';', '；'});
                foreach (var str in strs)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        var strinfo = str.Split(new []{ ':' , '：'});
                        if (strinfo.Length == 2)
                        {
                            var useDistance = 0;
                            int.TryParse(strinfo[1], out useDistance);
                            _flyIniInfos.Add(new FlyIniInfoItem(useDistance, Utils.GetMagic(strinfo[0], IsMagicFromCache)));
                        }
                    }
                }
            }
        }

        public Magic MagicToUseWhenLifeLow
        {
            get { return _magicToUseWhenLifeLow; }
            set
            {
                _magicToUseWhenLifeLow = value;
                if (_magicToUseWhenLifeLow != null)
                {
                    _magicToUseWhenLifeLow = _magicToUseWhenLifeLow.GetLevel(AttackLevel);
                }
            }
        }

        public int KeepRadiusWhenLifeLow
        {
            get { return _keepRadiusWhenLifeLow; }
            set { _keepRadiusWhenLifeLow = value; }
        }

        public int LifeLowPercent
        {
            get { return _lifeLowPercent; }
            set { _lifeLowPercent = value; }
        }

        public int KeepRadiusWhenFriendDeath
        {
            get { return _keepRadiusWhenFriendDeath; }
            set { _keepRadiusWhenFriendDeath = value; }
        }

        public Magic MagicToUseWhenBeAttacked
        {
            get { return _magicToUseWhenBeAttacked; }
            set { _magicToUseWhenBeAttacked = value; }
        }

        public int MagicDirectionWhenBeAttacked
        {
            get { return _magicDirectionWhenBeAttacked; }
            set { _magicDirectionWhenBeAttacked = value; }
        }

        public Magic MagicToUseWhenDeath
        {
            get { return _magicToUseWhenDeath; }
            set { _magicToUseWhenDeath = value; }
        }

        public int MagicDirectionWhenDeath
        {
            get { return _magicDirectionWhenDeath; }
            set { _magicDirectionWhenDeath = value; }
        }

        public MagicSprite LastAttackerMagicSprite;

        protected void AddMagicToInfos(Magic magic, int useDistance, bool notResort = false)
        {
            if (magic == null) return;
            _flyIniInfos.Add(new FlyIniInfoItem(useDistance, magic));

            if(! notResort) _flyIniInfos.Sort();
        }

        protected void RemoveMagicFromInfos(Magic magic, int useDistance)
        {
            if (magic == null) return;
            for (int i = 0; i < _flyIniInfos.Count; i++)
            {
                if (_flyIniInfos[i].TheMagic.FileName == magic.FileName && _flyIniInfos[i].UseDistance == useDistance)
                {
                    _flyIniInfos.RemoveAt(i);
                    break;
                }
            }
        }

        public string ScriptFile
        {
            get { return _scriptFile; }
            set { _scriptFile = value; }
        }

        public string TimerScriptFile
        {
            get { return _timerScriptFile; }
            set { _timerScriptFile = value; }
        }

        public int TimerScriptInterval
        {
            get { return _timerScriptInterval; }
            set { _timerScriptInterval = value; }
        }

        public bool HasInteractScript
        {
            get { return !string.IsNullOrEmpty(ScriptFile); }
        }

        public string DeathScript
        {
            get { return _deathScript; }
            set { _deathScript = value; }
        }

        public int ExpBonus
        {
            get { return _expBonus; }
            set { _expBonus = value; }
        }

        public string FixedPos
        {
            get { return _fixedPos; }
            set
            {
                _fixedPos = value;
                FixedPathTilePositionList = ToFixedPosTilePositionList(value);
            }
        }

        public int CurrentFixedPosIndex
        {
            get { return _currentFixedPosIndex; }
            set { _currentFixedPosIndex = value; }
        }

        public int Idle
        {
            get { return _idle; }
            set { _idle = value; }
        }

        public bool IsObstacle
        {
            get { return (Kind != 7); }
        }

        public bool IsPlayer
        {
            get { return (Kind == (int)CharacterKind.Player); }
        }

        public Vector2 DestinationMovePositionInWorld
        {
            get { return _destinationMovePositionInWorld; }
            set
            {
                _destinationMovePositionInWorld = value;
                _destinationMoveTilePosition = MapBase.ToTilePosition(value);
            }
        }

        public Vector2 DestinationMoveTilePosition
        {
            get
            { return _destinationMoveTilePosition; }
            set
            {
                _destinationMoveTilePosition = value;
                _destinationMovePositionInWorld = MapBase.ToPixelPosition(value);
            }
        }

        public Vector2 DestinationAttackTilePosition
        {
            get { return _destinationAttackTilePosition; }
            set
            {
                _destinationAttackTilePosition = value;
                _destinationAttackPositionInWorld = MapBase.ToPixelPosition(value);
            }
        }

        public Vector2 DestinationAttackPositionInWorld
        {
            get { return _destinationAttackPositionInWorld; }
            set
            {
                _destinationAttackPositionInWorld = value;
                _destinationAttackTilePosition = MapBase.ToTilePosition(value);
            }
        }

        public LinkedList<Vector2> Path
        {
            get { return _path; }
            protected set
            {
                _path = value;
                MovedDistance = 0;
            }
        }

        public bool IsDeath
        {
            get { return _isDeath; }
            protected set { _isDeath = value; }
        }

        /// <summary>
        /// Death() method invoked
        /// </summary>
        public bool IsDeathInvoked { protected set; get; }

        public bool IsInDeathing { get { return State == (int)CharacterState.Death; } }

        public MagicSprite SppedUpByMagicSprite { get; set; }

        #endregion Public properties

        #region Character Type and Relation
        public bool IsEnemy
        {
            get { return Kind == 1 && Relation == 1; }
        }

        public bool IsEventCharacter
        {
            get { return Kind == (int) CharacterKind.Eventer; }
        }

        public bool IsFriend
        {
            get { return Relation == (int)RelationType.Friend; }
        }

        public bool IsRelationNeutral { get { return Relation == 2; } }

        public bool IsFighterFriend
        {
            get { return ((Kind == 1 || Kind == 3) && Relation == 0); }
        }

        public bool IsFighter
        {
            get { return Kind == (int)CharacterKind.Fighter; }
        }

        public bool IsPartner
        {
            get { return Kind == 3; }
        }

        public bool IsInteractive
        {
            get { return (HasInteractScript || IsEnemy || IsFighterFriend); }
        }

       

        #endregion Character Type and Relation

        #region Ctor
        public Character() { }

        public Character(string filePath)
        {
            Load(filePath);
        }

        public Character(KeyDataCollection keyDataCollection)
        {
            Load(keyDataCollection);
        }
        #endregion Ctor

        #region Private method
        private void Initlize()
        {
            if (NpcIni.ContainsKey((int)CharacterState.Stand))
            {
                Set(MapBase.ToPixelPosition(MapX, MapY),
                    Globals.BaseSpeed,
                    NpcIni[(int)CharacterState.Stand].Image, Dir);
            }
            
            AddMagicToInfos(_flyIni, AttackRadius, true);
            AddMagicToInfos(_flyIni2, AttackRadius, true);
            _flyIniInfos.Sort();
            foreach (var flyIniInfoItem in _flyIniInfos)
            {
                flyIniInfoItem.SetLevel(AttackLevel);
            }

            if (_magicToUseWhenLifeLow != null)
            {
                _magicToUseWhenLifeLow = _magicToUseWhenLifeLow.GetLevel(AttackLevel);
            }

            if (_magicToUseWhenBeAttacked != null)
            {
                _magicToUseWhenBeAttacked = _magicToUseWhenBeAttacked.GetLevel(AttackLevel);
            }

            if (_magicToUseWhenDeath != null)
            {
                _magicToUseWhenDeath = _magicToUseWhenDeath.GetLevel(AttackLevel);
            }
        }

        /// <summary>
        /// Convert FixedPos string to path list
        /// FixedPos string pattern xx000000yy000000xx000000yy000000
        /// xx,yy is Hex string.
        /// xx - tile postion x, yy - tile postion y
        /// exemple: "0B0000001C0000000E000000170000000000000000000000"
        /// </summary>
        /// <param name="fixPos">FixedPos string</param>
        /// <returns>Converted path</returns>
        private List<Vector2> ToFixedPosTilePositionList(string fixPos)
        {
            var steps = Utils.SpliteStringInCharCount(fixPos, 8);
            var count = steps.Count;
            if (count < 4) return null; //Step less than 2
            if (count % 2 != 0) count--;//Not even, decrease one
            try
            {
                var path = new List<Vector2>();
                for (var i = 0; i < count; i += 2)
                {
                    var x = int.Parse(steps[i].Substring(0, 2), NumberStyles.HexNumber);
                    var y = int.Parse(steps[i + 1].Substring(0, 2), NumberStyles.HexNumber);
                    if (x == 0 && y == 0) break;
                    path.Add(new Vector2(x, y));
                }
                return path;
            }
            catch (Exception)
            {
                //FixedPos format error
                return null;
            }
        }

        private void EndInteract()
        {
            _isInInteract = false;
            if (IsStanding())
            {
                //Character may moved when in interacting.
                //Only set back direction when standing.
                SetDirection(_directionBeforInteract);
            }
        }

        private void CheckStepMove()
        {
            if (!_isInStepMove) return;
            if (_leftStepToMove == 0)
            {
                StandingImmediately();
                _isInStepMove = false;
                return;
            }
            var destinationTilePosition = Engine.PathFinder.FindNeighborInDirection(
                TilePosition,
                _stepMoveDirection);
            WalkTo(destinationTilePosition);
            if (Path == null ||
                HasObstacle(destinationTilePosition))//Unable to move
            {
                _isInStepMove = false;
                return;
            }
            _leftStepToMove--;
        }

        private void MoveAlongPath(float elapsedSeconds, int speedFold)
        {
            if (Path == null || Path.Count < 2)
            {
                StandingImmediately();
                return;
            }

            var from = Path.First.Value;
            var to = Path.First.Next.Value;
            var tileFrom = MapBase.ToTilePosition(from);
            var tileTo = MapBase.ToTilePosition(to);
            var direction = to - from;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
            }
            var distance = Vector2.Distance(from, to);
            //Sotre value in case of cleared in below code
            var interactTarget = _interactiveTarget;

            if (TilePosition == tileFrom && tileFrom != tileTo)
            {
                if (HasObstacle(tileTo)) //Obstacle in the way
                {
                    MovedDistance = 0;

                    //PositionInWorld = from;
                    if (tileTo == DestinationMoveTilePosition) //Just one step, standing
                    {
                        Path = null;
                        StandingImmediately();
                        return;
                    }
                    else if (PositionInWorld == MapBase.ToPixelPosition(TilePosition)) //At tile center, find new path
                    {
                        var path = Engine.PathFinder.FindPath(this, TilePosition, DestinationMoveTilePosition, PathType);
                        if (path == null)
                        {
                            Path = null;
                            StandingImmediately();
                        }
                        else
                        {
                            Path = path;
                        }
                    }
                    else
                    {
                        // Move bact to tile center
                        var path = new LinkedList<Vector2>();
                        path.AddLast(PositionInWorld);
                        path.AddLast(MapBase.ToPixelPosition(TilePosition));
                        Path = path;
                    }
                    return;
                }
            }
            MoveTo(direction, elapsedSeconds * speedFold);
            if (TilePosition != tileFrom &&
                TilePosition != tileTo) // neither in from nor in to, correcting path
            {
                if (MovedDistance >= distance)
                {
                    MovedDistance = distance;
                    PositionInWorld = to;
                }
                else
                {
                    var correctDistance = distance / 2 + 1f; // half plus one
                    PositionInWorld = from + direction * correctDistance;
                    MovedDistance = correctDistance;
                }

            }

            if (MovedDistance >= distance)
            {
                if (_isNextStepStand)
                {
                    _isNextStepStand = false;
                    PositionInWorld = to;
                    StandingImmediately();
                    MovedDistance = 0;
                }
                else if ((PathType == Engine.PathFinder.PathType.PathOneStep && Path.Count <= 2) ||//Step path end
                    (PathType != Engine.PathFinder.PathType.PathOneStep && DestinationMovePositionInWorld != Path.Last.Value) ||// new destination
                    MoveTargetChanged)
                {
                    var destination = DestinationMovePositionInWorld;
                    PositionInWorld = to;
                    Path = Engine.PathFinder.FindPath(this, TilePosition, MapBase.ToTilePosition(destination), PathType);
                    if (Path == null) StandingImmediately();

                    if (MoveTargetChanged)
                    {
                        MoveTargetChanged = false;
                    }
                }
                else
                {
                    if (Path.Count > 2)
                    {
                        Path.RemoveFirst();
                        //Apply offset to next move
                        var offset = MovedDistance - distance;
                        var newFrom = Path.First.Value;
                        var newTo = Path.First.Next.Value;
                        var newDirection = newTo - newFrom;
                        newDirection.Normalize();
                        PositionInWorld = newFrom + newDirection * offset;
                        MovedDistance = offset;
                    }
                    else
                    {
                        PositionInWorld = to;
                        //If in step move don't stand
                        if (!IsInStepMove)
                        {
                            StandingImmediately();
                        }
                        MovedDistance = 0;
                    }
                }
                CheckMapTrap();
                CheckStepMove();
                Magic magicToUse;
                if (AttackingIsOk(out magicToUse)) PerformeAttack(magicToUse);
                _interactiveTarget = interactTarget;
                if (InteractIsOk()) PerformeInteract();
                if (IsRuning())
                {
                    if (!CanRunning())
                    {
                        WalkTo(DestinationMoveTilePosition);
                    }
                }
            }
        }

        private void JumpAlongPath(float elapsedSeconds)
        {
            if (Path == null)
            {
                StandingImmediately();
                return;
            }
            if (Path.Count == 2)
            {
                var from = Path.First.Value;
                var to = Path.First.Next.Value;
                var distance = Vector2.Distance(from, to);
                bool isOver = false;
                var nextTile = Engine.PathFinder.FindNeighborInDirection(
                    TilePosition, to - from);
                if (MapBase.Instance.IsObstacleForCharacterJump(nextTile) ||
                    (nextTile == MapBase.ToTilePosition(to) && HasObstacle(nextTile)) ||
                    MapBase.Instance.HasTrapScript(TilePosition) ||
                    NpcManager.GetEventer(nextTile) != null)
                {
                    TilePosition = TilePosition;//Correcting position
                    isOver = true;
                }
                else MoveTo(to - from, elapsedSeconds * 8);
                if (MovedDistance >= distance - Globals.DistanceOffset && !isOver)
                {
                    MovedDistance = 0;
                    PositionInWorld = to;
                    isOver = true;
                }
                if (isOver)
                {
                    Path.RemoveFirst();
                }
            }
            if (IsPlayCurrentDirOnceEnd())
            {
                StandingImmediately();
                CheckMapTrap();
            }
        }

        protected void RandWalk(List<Vector2> tilePositionList, int randMaxValue, bool isFlyer)
        {
            if (tilePositionList == null ||
                tilePositionList.Count < 2 ||
                !IsStanding() ||
                _isInInteract) return;
            if (Globals.TheRandom.Next(0, randMaxValue) == 0)
            {
                var tilePosition = tilePositionList[Globals.TheRandom.Next(0, tilePositionList.Count)];
                MoveTo(tilePosition, isFlyer);
            }
        }

        protected void LoopWalk(List<Vector2> tilePositionList, int randMaxValue, ref int currentPathIndex, bool isFlyer)
        {
            if (tilePositionList == null ||
                tilePositionList.Count < 2 ||
                _isInInteract) return;

            IsInLoopWalk = true;

            if (IsStanding() &&
                Globals.TheRandom.Next(0, randMaxValue) == 0)
            {
                currentPathIndex++;
                if (currentPathIndex > tilePositionList.Count - 1)
                {
                    currentPathIndex = 0;
                }
                MoveTo(tilePositionList[currentPathIndex], isFlyer);
            }
        }

        protected void KeepMinTileDistance(Vector2 targetTilePosition, int minTileDistance)
        {
            if (_isInInteract) // In interacting can't moving
            {
                return;
            }

            var tileDistance = Engine.PathFinder.GetViewTileDistance(TilePosition, targetTilePosition);

            if (tileDistance < minTileDistance && IsStanding())
            {
                MoveAwayTarget(MapBase.ToPixelPosition(targetTilePosition), minTileDistance - tileDistance, false);
            }
        }

        protected void MoveTo(Vector2 tilePosition, bool isFlyer)
        {
            if (isFlyer)
            {
                //Flyer can move in straight line use fixed path move style.
                FixedPathMoveToDestination(tilePosition);
            }
            else
            {
                //Find path and walk to destionation.
                WalkTo(tilePosition);
            }
        }

        /// <summary>
        /// Get rand path, path first step is current character tile position.
        /// </summary>
        /// <param name="count">Path step count.</param>
        /// <param name="isFlyer">If false, tile position is obstacle for character is no added to path.</param>
        /// <returns>The rand path.</returns>
        protected List<Vector2> GetRandTilePath(int count, bool isFlyer)
        {
            var path = new List<Vector2>() { TilePosition };

            var maxTry = count * 3;//For performace, otherwise method may run forever.
            var maxOffset = 10;
            if (isFlyer)
            {
                maxOffset = 15;
            }

            for (var i = 1; i < count; i++)
            {
                Vector2 tilePosition;
                do
                {
                    if (--maxTry < 0) return path;

                    tilePosition = MapBase.Instance.GetRandPositon(TilePosition, maxOffset);
                } while (tilePosition == Vector2.Zero ||
                    (!isFlyer && Engine.PathFinder.HasMapObstacalInTilePositionList(
                    Engine.PathFinder.GetLinePath(
                    TilePosition,
                    tilePosition,
                    maxOffset * 2,
                    true))));
                path.Add(tilePosition);
            }

            return path;
        }

        /// <summary>
        /// Move to destination in fixed path move style.
        /// </summary>
        /// <param name="destinationTilePosition">Move destination tile position.</param>
        protected void FixedPathMoveToDestination(Vector2 destinationTilePosition)
        {
            _fixedPathMoveDestinationPixelPostion = MapBase.ToPixelPosition(destinationTilePosition);
            _fixedPathDistanceToMove = Vector2.Distance(PositionInWorld, _fixedPathMoveDestinationPixelPostion);
            MovedDistance = 0f;
            if (_fixedPathDistanceToMove < 1)
            {
                return;
            }
            SetState(CharacterState.Walk);
        }
        #endregion Private method

        #region Protected method

        protected virtual void AssignToValue(KeyData keyData)
        {
            try
            {
                var info = GetType().GetProperty(keyData.KeyName);
                switch (keyData.KeyName)
                {
                    case "FixedPos":
                    case "Name":
                    case "ScriptFile":
                    case "DeathScript":
                    case "TimerScriptFile":
                    case "FlyInis":
                    case "DropIni":
                        info.SetValue(this, keyData.Value, null);
                        break;
                    case "NpcIni":
                        SetNpcIni(keyData.Value);
                        break;
                    case "BodyIni":
                        if (!string.IsNullOrEmpty(keyData.Value))
                        {
                            info.SetValue(this, new Obj(@"ini\obj\" + keyData.Value), null);
                        }
                        break;
                    case "Defence":
                        Defend = int.Parse(keyData.Value);
                        break;
                    case "LevelIni":
                        LevelIniFile = keyData.Value;
                        info.SetValue(this, Utils.GetLevelLists(@"ini\level\" + keyData.Value), null);
                        break;
                    case "FlyIni":
                        _flyIni = Utils.GetMagic(keyData.Value, IsMagicFromCache);
                        break;
                    case "FlyIni2":
                        _flyIni2 = Utils.GetMagic(keyData.Value, IsMagicFromCache);
                        break;
                    case "MagicToUseWhenLifeLow":
                        _magicToUseWhenLifeLow = Utils.GetMagic(keyData.Value, IsMagicFromCache);
                        break;
                    case "MagicToUseWhenBeAttacked":
                        _magicToUseWhenBeAttacked = Utils.GetMagic(keyData.Value, IsMagicFromCache);
                        break;
                    case "MagicToUseWhenDeath":
                        _magicToUseWhenDeath = Utils.GetMagic(keyData.Value, IsMagicFromCache);
                        break;
                    case "Life":
                        _life = int.Parse(keyData.Value);
                        break;
                    case "Thew":
                        _thew = int.Parse(keyData.Value);
                        break;
                    case "Mana":
                        _mana = int.Parse(keyData.Value);
                        break;
                    case "PoisonSeconds":
                    case "PetrifiedSeconds":
                    case "FrozenSeconds":
                        info.SetValue(this, float.Parse(keyData.Value), null);
                        break;
                    default:
                        {
                            var integerValue = int.Parse(keyData.Value);
                            info.SetValue(this, integerValue, null);
                            break;
                        }
                }
            }
            catch (Exception)
            {
                //Do nothing
                return;
            }
        }

        /// <summary>
        /// Check wheather follow target is findable.Is no follow target do nothing.
        /// Dirived class can override <see cref="FollowTargetFound"/> <see cref="FollowTargetLost"/> method to change character behaviour.
        /// </summary>
        protected void PerformeFollow()
        {
            if (FollowTarget == null) return;
            var targetTilePosition = FollowTarget.TilePosition;

            var tileDistance = Engine.PathFinder.GetViewTileDistance(TilePosition, targetTilePosition);

            var canSeeTarget = false;

            if (tileDistance <= VisionRadius) //Target in view range
            {
                canSeeTarget = Engine.PathFinder.CanViewTarget(TilePosition, targetTilePosition, VisionRadius);

                IsFollowTargetFound = (IsFollowTargetFound || //Target already found and within vision radius
                                       canSeeTarget); //Character can see target
            }
            else
            {
                IsFollowTargetFound = false;
            }

            if (IsFollowTargetFound)
            {
                FollowTargetFound(canSeeTarget);
            }
            else
            {
                FollowTargetLost();
            }
        }

        protected virtual void FollowTargetFound(bool attackCanReach)
        {
            WalkTo(FollowTarget.TilePosition);
        }

        protected virtual void FollowTargetLost()
        {
            //Do nothing
        }

        protected virtual void CheckMapTrap() { }

        protected abstract void PlaySoundEffect(SoundEffect soundEffect);

        protected virtual bool CanUseMagic()
        {
            return true;
        }

        protected virtual bool CanRunning()
        {
            return !IsRunDisabled;
        }

        protected virtual bool CanJump()
        {
            return !IsJumpDisabled && NpcIni.ContainsKey((int)CharacterState.Jump);
        }

        protected void AddKey(KeyDataCollection keyDataCollection, string key, float value)
        {
            if (value > 0)
            {
                keyDataCollection.AddKey(key, value.ToString());
            }
        }

        protected void AddKey(KeyDataCollection keyDataCollection, string key, int value, int defaultValue = 0)
        {
            if (value != defaultValue)
            {
                keyDataCollection.AddKey(key, value.ToString());
            }
        }

        protected void AddKey(KeyDataCollection keyDataCollection, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                keyDataCollection.AddKey(key, value);
            }
        }

        protected void AddKey(KeyDataCollection keyDataCollection, string key, bool value)
        {
            if (value)
            {
                keyDataCollection.AddKey(key, "1");
            }
        }

        /// <summary>
        /// Set state to current state
        /// </summary>
        protected void EndSpecialAction()
        {
            SetState((CharacterState)State, true, true);
        }

        /// <summary>
        /// Override this to do something when attacking(use magic FlyIni FlyIni2).
        /// </summary>
        /// <param name="attackDestinationPixelPosition">Attacking destination</param>
        protected virtual void OnAttacking(Vector2 attackDestinationPixelPosition)
        {
            //Do nothing here
        }

        /// <summary>
        /// Add extra behaviour when character begin sit down
        /// </summary>
        protected virtual void OnSitDown()
        {
            //do nothing
        }
        #endregion Protected method

        #region Public method
        public abstract bool HasObstacle(Vector2 tilePosition);
        #endregion Public method

        #region Save load method
        public bool Load(string filePath)
        {
            try
            {
                var data = new FileIniDataParser().ReadFile(filePath, Globals.LocalEncoding);
                return Load(Utils.GetFirstSection(data));
            }
            catch (Exception exception)
            {
                Log.LogFileLoadError("Character", filePath, exception);
                return false;
            }
        }

        public bool Load(KeyDataCollection keyDataCollection)
        {
            if (_npcIni != null)
            {
                _npcIni.Clear();
            }
            foreach (var keyData in keyDataCollection)
            {
                AssignToValue(keyData);
            }
            Initlize();
            return true;
        }

        /// <summary>
        /// Save to ini
        /// </summary>
        /// <param name="keyDataCollection">Ini key value collection</param>
        public virtual void Save(KeyDataCollection keyDataCollection)
        {
            keyDataCollection.AddKey("Name", _name);
            AddKey(keyDataCollection, "Kind", _kind);
            AddKey(keyDataCollection, "Relation", _relation);
            AddKey(keyDataCollection, "PathFinder", _pathFinder);
            AddKey(keyDataCollection, "State", _state);
            AddKey(keyDataCollection, "VisionRadius", _visionRadius);
            AddKey(keyDataCollection, "DialogRadius", _dialogRadius);
            AddKey(keyDataCollection, "AttackRadius", _attackRadius);
            AddKey(keyDataCollection, "Dir", _dir);
            keyDataCollection.AddKey("MapX", MapX.ToString());
            keyDataCollection.AddKey("MapY", MapY.ToString());
            AddKey(keyDataCollection, "Lum", _lum);
            AddKey(keyDataCollection, "Action", _action);
            AddKey(keyDataCollection, "WalkSpeed", _walkSpeed);
            AddKey(keyDataCollection, "Evade", _evade);
            AddKey(keyDataCollection, "Attack", _attack);
            AddKey(keyDataCollection, "Attack2", _attack2);
            AddKey(keyDataCollection, "AttackLevel", _attackLevel);
            AddKey(keyDataCollection, "Defend", _defend);
            AddKey(keyDataCollection, "Defend2", _defend2);
            AddKey(keyDataCollection, "Exp", _exp);
            AddKey(keyDataCollection, "LevelUpExp", _levelUpExp);
            AddKey(keyDataCollection, "Level", _level);
            AddKey(keyDataCollection, "Life", _life);
            AddKey(keyDataCollection, "LifeMax", _lifeMax);
            AddKey(keyDataCollection, "Thew", _thew);
            AddKey(keyDataCollection, "ThewMax", _thewMax);
            AddKey(keyDataCollection, "Mana", _mana);
            AddKey(keyDataCollection, "ManaMax", _manaMax);
            AddKey(keyDataCollection, "ExpBonus", _expBonus);
            AddKey(keyDataCollection, "FixedPos", _fixedPos);
            AddKey(keyDataCollection, "CurrentFixedPosIndex", CurrentFixedPosIndex);
            AddKey(keyDataCollection, "Idle", _idle);
            AddKey(keyDataCollection, "NpcIni", _npcIniFileName);
            AddKey(keyDataCollection, "PoisonSeconds", _poisonSeconds);
            AddKey(keyDataCollection, "PetrifiedSeconds", _petrifiedSeconds);
            AddKey(keyDataCollection, "FrozenSeconds", _frozenSeconds);
            if (_bodyIni != null)
            {
                AddKey(keyDataCollection,
                    "BodyIni",
                    _bodyIni == null ? string.Empty : _bodyIni.FileName);
            }
            if (_flyIni != null)
            {
                AddKey(keyDataCollection, "FlyIni", _flyIni.FileName);
            }
            if (_flyIni2 != null)
            {
                AddKey(keyDataCollection, "FlyIni2", _flyIni2.FileName);
            }
            AddKey(keyDataCollection, "FlyInis", _flyInis);
            if (_magicToUseWhenLifeLow != null)
            {
                AddKey(keyDataCollection, "MagicToUseWhenLifeLow", _magicToUseWhenLifeLow.FileName);
            }
            AddKey(keyDataCollection, "LifeLowPercent", _lifeLowPercent, LifeLowPercentDefault);
            AddKey(keyDataCollection, "KeepRadiusWhenLifeLow", _keepRadiusWhenLifeLow);
            AddKey(keyDataCollection, "KeepRadiusWhenFriendDeath", _keepRadiusWhenFriendDeath);
            if (_magicToUseWhenBeAttacked != null)
            {
                AddKey(keyDataCollection, "MagicToUseWhenBeAttacked", _magicToUseWhenBeAttacked.FileName);
            }
            AddKey(keyDataCollection, "MagicDirectionWhenBeAttacked", _magicDirectionWhenBeAttacked);
            if (_magicToUseWhenDeath != null)
            {
                AddKey(keyDataCollection, "MagicToUseWhenDeath", _magicToUseWhenDeath.FileName);
            }
            AddKey(keyDataCollection, "MagicDirectionWhenDeath", _magicDirectionWhenDeath);
            if (_scriptFile != null)
            {
                AddKey(keyDataCollection, "ScriptFile", _scriptFile);
            }
            if (_deathScript != null)
            {
                AddKey(keyDataCollection, "DeathScript", _deathScript);
            }
            if (_timerScriptFile != null)
            {
                AddKey(keyDataCollection, "TimerScriptFile", _timerScriptFile);
            }
            if (_timerScriptInterval != Globals.DefaultNpcObjTimeScriptInterval)
            {
                AddKey(keyDataCollection, "TimerScriptInterval", _timerScriptInterval);
            }
            AddKey(keyDataCollection, "DropIni", _dropIni);
        }

        #endregion Save load method

        #region Perform action
        public void StandingImmediately()
        {
            if (IsDeathInvoked || IsDeath)
            {
                return;
            }
            StateInitialize(false);
            if (_isInFighting && NpcIni.ContainsKey((int)CharacterState.FightStand)) SetState(CharacterState.FightStand);
            else
            {
                if (NpcIni.ContainsKey((int)CharacterState.Stand1) &&
                    Globals.TheRandom.Next(4) == 1 &&
                    State != (int)CharacterState.Stand1) SetState(CharacterState.Stand1);
                else SetState(CharacterState.Stand);
                PlayCurrentDirOnce();
            }
        }

        /// <summary>
        /// If is in walk or run, when reach next step(tile), standing.
        /// Don't like StandingImmediately() staning now,  this is a soft standing
        /// </summary>
        public void NextStepStaning()
        {
            if (IsWalking() || IsRuning())
            {
                _isNextStepStand = true;
            }
        }

        public bool PerformActionOk()
        {
            if (State == (int)CharacterState.Jump ||
                State == (int)CharacterState.Attack ||
                State == (int)CharacterState.Attack1 ||
                State == (int)CharacterState.Attack2 ||
                State == (int)CharacterState.Magic ||
                State == (int)CharacterState.Hurt ||
                State == (int)CharacterState.Death ||
                State == (int)CharacterState.FightJump ||
                IsPetrified ||
                IsInTransport ||
                MovedByMagicSprite != null ||
                BouncedVelocity > 0) return false;
            return true;
        }

        /// <summary>
        /// Before set character state initialize character state. 
        /// </summary>
        /// <param name="endInteract">Is true end interact if character is in interacting.</param>
        public void StateInitialize(bool endInteract = true)
        {
            EndPlayCurrentDirOnce();
            DestinationMoveTilePosition = Vector2.Zero;
            Path = null;
            CancleAttackTarget();
            IsSitted = false;
            if (_isInInteract && endInteract)
            {
                //End interact in case of action to perform direction not correct
                EndInteract();
            }
        }

        public bool IsStanding()
        {
            return (State == (int)CharacterState.Stand ||
                    State == (int)CharacterState.Stand1 ||
                    State == (int)CharacterState.FightStand);
        }

        public bool IsWalking()
        {
            return (State == (int)CharacterState.Walk ||
                    State == (int)CharacterState.FightWalk);
        }

        public bool IsRuning()
        {
            return (State == (int)CharacterState.Run ||
                    State == (int)CharacterState.FightRun);
        }

        public bool IsSitting()
        {
            return State == (int)CharacterState.Sit;
        }

        /// <summary>
        /// Walk steps in direction.
        /// </summary>
        /// <param name="direction">Direction 0-7</param>
        /// <param name="moveStep">Steps to move</param>
        public void WalkToDirection(int direction, int moveStep)
        {
            _isInStepMove = true;
            _leftStepToMove = moveStep;
            _stepMoveDirection = direction;
            CheckStepMove();
        }

        /// <summary>
        /// Walk to destination tile position.
        /// </summary>
        /// <param name="destinationTilePosition">Destination tile positon</param>
        /// <param name="pathType">PathType to use, if type is End, use character defult.</param>
        public virtual void WalkTo(Vector2 destinationTilePosition, PathFinder.PathType pathType = Engine.PathFinder.PathType.End)
        {
            if (PerformActionOk() &&
                destinationTilePosition != TilePosition)
            {
                //If in step move, alway find new path
                if (IsWalking() && !IsInStepMove)
                {
                    DestinationMoveTilePosition = destinationTilePosition;
                    CancleAttackTarget();
                }
                else
                {
                    StateInitialize();
                    Path = Engine.PathFinder.FindPath(this, TilePosition, destinationTilePosition, pathType == Engine.PathFinder.PathType.End ? PathType : pathType);
                    if (Path == null) StandingImmediately();
                    else
                    {
                        DestinationMoveTilePosition = destinationTilePosition;
                        if (_isInFighting && NpcIni.ContainsKey((int)CharacterState.FightWalk)) SetState(CharacterState.FightWalk);
                        else SetState(CharacterState.Walk);
                    }
                }
            }

        }

        public virtual void RunTo(Vector2 destinationTilePosition, PathFinder.PathType pathType = Engine.PathFinder.PathType.End)
        {
            if (PerformActionOk() &&
                destinationTilePosition != TilePosition)
            {
                if (!NpcIni.ContainsKey((int) CharacterState.Run))
                {
                    return;
                }
                if (IsRuning())
                    DestinationMoveTilePosition = destinationTilePosition;
                else
                {
                    StateInitialize();
                    Path = Engine.PathFinder.FindPath(this, TilePosition, destinationTilePosition, pathType == Engine.PathFinder.PathType.End ? PathType : pathType);
                    if (Path == null) StandingImmediately();
                    else
                    {
                        DestinationMoveTilePosition = destinationTilePosition;
                        if (_isInFighting && NpcIni.ContainsKey((int)CharacterState.FightRun)) SetState(CharacterState.FightRun);
                        else SetState(CharacterState.Run);
                    }
                }

            }
        }

        public void JumpTo(Vector2 destinationTilePosition)
        {
            if (PerformActionOk() &&
                destinationTilePosition != TilePosition &&
                !MapBase.Instance.IsObstacleForCharacter(destinationTilePosition) &&
                !HasObstacle(destinationTilePosition))
            {
                if (!CanJump()) return;
                StateInitialize();
                DestinationMoveTilePosition = destinationTilePosition;
                Path = new LinkedList<Vector2>();
                Path.AddLast(PositionInWorld);
                Path.AddLast(DestinationMovePositionInWorld);

                if (_isInFighting && NpcIni.ContainsKey((int)CharacterState.FightJump)) SetState(CharacterState.FightJump);
                else SetState(CharacterState.Jump);
                SetDirection(DestinationMovePositionInWorld - PositionInWorld);
                PlayCurrentDirOnce();
            }
        }

        public void Sitdown()
        {
            if (PerformActionOk())
            {
                StateInitialize();
                if (NpcIni.ContainsKey((int)CharacterState.Sit))
                {
                    SetState(CharacterState.Sit);
                    PlayFrames(FrameEnd - FrameBegin);
                }
                OnSitDown();
            }
        }

        protected virtual void MagicUsedHook(Magic magic)
        {
            
        }

        public void UseMagic(Magic magicUse, Vector2 magicDestinationTilePosition, Character target = null)
        {
            if (PerformActionOk())
            {
                StateInitialize();
                ToFightingState();

                MagicUse = magicUse;
                _magicDestination = MapBase.ToPixelPosition(magicDestinationTilePosition);
                _magicTarget = target;
                SetState(CharacterState.Magic);

                if (magicUse.UseActionFile != null)
                {
                    Texture = magicUse.UseActionFile;
                }

                SetDirection(_magicDestination - PositionInWorld);
                PlayCurrentDirOnce();
            }
        }

        /// <summary>
        /// Character is hurting.
        /// Depending on posibility, character may or may not change to hurting state.
        /// When character is in pertrified state, hurting is ignored.
        /// </summary>
        public void Hurting()
        {
            const int maxRandValue = 4;
            if (Globals.TheRandom.Next(maxRandValue) != 0 ||
                IsPetrified) //Can't hurted when been petrified for game playability
            {
                return;
            }

            if (State != (int)CharacterState.Death &&
                State != (int)CharacterState.Hurt &&
                !IsPetrified)
            {
                StateInitialize();
                TilePosition = TilePosition;//To tile center
                if (NpcIni.ContainsKey((int)CharacterState.Hurt))
                {
                    SetState(CharacterState.Hurt);
                    PlayCurrentDirOnce();
                }
            }
        }

        private static Asf FrozenDie;
        private static Asf PoisonDie;
        private static Asf PetrifiedDie;
        public virtual void Death()
        {
            if (IsDeathInvoked) return;
            IsDeathInvoked = true;

            NpcManager.AddDead(this);

            //When death speedup is cancled
            SppedUpByMagicSprite = null;

            //Run death script
            _currentRunDeathScript = ScriptManager.RunScript(Utils.GetScriptParser(DeathScript), this);

            if (MagicToUseWhenDeath != null)
            {
                Vector2 destination;
                Character target = null;
                var dirType = (DeathUseMagicDirection)MagicDirectionWhenDeath;
                switch (dirType)
                {
                    case DeathUseMagicDirection.LastAttacker:
                        destination = LastAttackerMagicSprite == null ? 
                            PositionInWorld + Utils.GetDirection8(CurrentDirection) : 
                            LastAttackerMagicSprite.BelongCharacter.PositionInWorld;
                        target = LastAttackerMagicSprite == null ? null : LastAttackerMagicSprite.BelongCharacter;
                        break;
                    case DeathUseMagicDirection.LastMagicSpriteOppDirection:
                        destination = (LastAttackerMagicSprite == null || LastAttackerMagicSprite.RealMoveDirection == Vector2.Zero)
                            ? (PositionInWorld + Utils.GetDirection8(CurrentDirection))
                            : (PositionInWorld - LastAttackerMagicSprite.RealMoveDirection);
                        break;
                    case DeathUseMagicDirection.CurrentNpcDirection:
                        destination = PositionInWorld + Utils.GetDirection8(CurrentDirection);
                        break;
                    default:
                        destination = PositionInWorld + Utils.GetDirection8(CurrentDirection);
                        break;
                }
                MagicManager.UseMagic(this, MagicToUseWhenDeath, PositionInWorld, destination, target);
            }

            if (ControledMagicSprite != null)
            {
                //End control by other
                var player = ControledMagicSprite.BelongCharacter as Player;
                if (player == null)
                {
                    throw new Exception("Magic kind 21 internal error.");
                }
                player.EndControlCharacter();
            }

            if (SummonedByMagicSprite != null)
            {
                //Npc is been summoned by others.
                IsDeath = true;
                if (!SummonedByMagicSprite.IsInDestroy && !SummonedByMagicSprite.IsDestroyed)
                {
                    //Character death before magic sprite destory
                    SummonedByMagicSprite.Destroy();
                }
                return;
            }

            StateInitialize();
            if (NpcIni.ContainsKey((int)CharacterState.Death))
            {
                SetState(CharacterState.Death);
                if (IsFrozened)
                {
                    if (FrozenDie == null) FrozenDie = Utils.GetAsf(@"asf\interlude\", "die-冰.asf");
                    Texture = FrozenDie;
                    CurrentDirection = 0;
                    _notAddBody = true;
                }
                else if (IsPoisoned)
                {
                    if (PoisonDie == null) PoisonDie = Utils.GetAsf(@"asf\interlude\", "die-毒.asf");
                    Texture = PoisonDie;
                    CurrentDirection = 0;
                    _notAddBody = true;
                }
                else if (IsPetrified)
                {
                    if (PetrifiedDie == null) PetrifiedDie = Utils.GetAsf(@"asf\interlude\", "die-石.asf");
                    Texture = PetrifiedDie;
                    CurrentDirection = 0;
                    _notAddBody = true;
                }
                ToNormalState();
                PlayCurrentDirOnce();
            }
            else IsDeath = true;
        }

        public void Attacking(Vector2 destinationTilePosition, bool isRun = false)
        {
            if (PerformActionOk())
            {
                _isRunToTarget = isRun;
                DestinationAttackTilePosition = destinationTilePosition;
                Magic magicToUse;
                if (IsStanding() && AttackingIsOk(out magicToUse))
                    PerformeAttack(magicToUse);
            }
        }

        public void CancleAttackTarget()
        {
            DestinationAttackTilePosition = Vector2.Zero;
            _interactiveTarget = null;
        }

        /// <summary>
        /// Get closest use magic distance form magic info list.
        /// </summary>
        /// <param name="toTargetDistance">Current distance to target.</param>
        /// <returns></returns>
        protected int GetClosedAttackRadius(int toTargetDistance)
        {
            var minDistance = int.MaxValue;
            var result = 0;
            for (var i = 0; i < _flyIniInfos.Count; i++)
            {
                var distance = Math.Abs(toTargetDistance - _flyIniInfos[i].UseDistance);
                if (minDistance > distance)
                {
                    minDistance = distance;
                    result = _flyIniInfos[i].UseDistance;
                }
                if (_flyIniInfos[i].UseDistance > toTargetDistance) break;
            }
            return result;
        }

        /// <summary>
        /// Get a random magic which use distance equal useDistance. 
        /// If not find return smallest use distace magic.
        /// Otherwise return null.
        /// </summary>
        /// <param name="useDistance"></param>
        /// <returns></returns>
        protected Magic GetRamdomMagicWithUseDistance(int useDistance)
        {
            var start = -1;
            var end = -1;
            for (var i = 0; i < _flyIniInfos.Count; i++)
            {
                if (useDistance == _flyIniInfos[i].UseDistance)
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                }
                else
                {
                    if (start != -1)
                    {
                        end = i;
                        break;
                    }
                }
            }
            if (end == -1) end = _flyIniInfos.Count;
            if (start != -1)
            {
                var index = start + Globals.TheRandom.Next(end - start);
                return _flyIniInfos[index].TheMagic;
            }
            for (var i = 0; i < _flyIniInfos.Count; i++)
            {
                if (_flyIniInfos[i].UseDistance > useDistance)
                {
                    return _flyIniInfos[i].TheMagic;
                }
            }
            return _flyIniInfos.Count > 0 ? _flyIniInfos[0].TheMagic : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="useAttackDistance">If return true, its value is the magic to use when attack.</param>
        /// <returns></returns>
        protected bool AttackingIsOk(out Magic magicToUse)
        {
            magicToUse = null;
            if (DestinationAttackTilePosition != Vector2.Zero)
            {
                int tileDistance = Engine.PathFinder.GetViewTileDistance(TilePosition, DestinationAttackTilePosition);
                var attackRadius = GetClosedAttackRadius(tileDistance);

                if (tileDistance == attackRadius)
                {
                    var canSeeTarget = Engine.PathFinder.CanViewTarget(TilePosition,
                        DestinationAttackTilePosition,
                        tileDistance);

                    if (canSeeTarget)
                    {
                        magicToUse = GetRamdomMagicWithUseDistance(attackRadius);
                        return true;
                    }

                    MoveToTarget(DestinationAttackTilePosition, _isRunToTarget);
                }
                if (tileDistance > attackRadius)
                {
                    MoveToTarget(DestinationAttackTilePosition, _isRunToTarget);
                }
                else
                {
                    //Actack distance too small, move away target
                    if (!MoveAwayTarget(DestinationAttackPositionInWorld,
                        attackRadius - tileDistance,
                        _isRunToTarget))
                    {
                        magicToUse = GetRamdomMagicWithUseDistance(attackRadius);
                        return true;
                    }
                }
            }
            return false;
        }

        protected bool MoveAwayTarget(Vector2 targetPositionInWorld, int awayTileDistance, bool isRun)
        {
            if (awayTileDistance < 1) return false;

            var neighbor = Engine.PathFinder.FindDistanceTileInDirection(
                TilePosition,
                PositionInWorld - targetPositionInWorld,
                awayTileDistance);

            if (HasObstacle(neighbor)) return false;

            MoveToTarget(neighbor, isRun);
            if (Path == null)
            {
                //Can't find path.
                return false;
            }
            return true;
        }

        private void MoveToTarget(Vector2 destinationTilePosition, bool isRun)
        {
            if (isRun)
                RunToAndKeepingTarget(destinationTilePosition);
            else
                WalkToAndKeepingTarget(destinationTilePosition);
        }

        protected void WalkToAndKeepingTarget(Vector2 destinationTilePosition)
        {
            //keep value
            var attack = DestinationAttackTilePosition;
            var interact = _interactiveTarget;
            WalkTo(destinationTilePosition);
            // restore
            DestinationAttackTilePosition = attack;
            _interactiveTarget = interact;
        }

        protected void RunToAndKeepingTarget(Vector2 destinationTilePosition)
        {
            //keep value
            var attack = DestinationAttackTilePosition;
            var interact = _interactiveTarget;
            RunTo(destinationTilePosition);
            // restore
            DestinationAttackTilePosition = attack;
            _interactiveTarget = interact;
        }

        protected void PerformeAttack(Magic magicToUse)
        {
            PerformeAttack(DestinationAttackPositionInWorld, magicToUse);
        }

        protected virtual bool CanPerformeAttack()
        {
            return true;
        }

        protected virtual void OnPerformeAttack()
        {

        }

        public void PerformeAttack(Vector2 destinationPositionInWorld)
        {
            PerformeAttack(destinationPositionInWorld, GetRamdomMagicWithUseDistance(AttackRadius));
        }

        public void PerformeAttack(Vector2 destinationPositionInWorld, Magic magicToUse)
        {
            if (PerformActionOk())
            {
                if (!CanPerformeAttack()) return;
                StateInitialize();
                ToFightingState();
                _attackDestination = destinationPositionInWorld;
                _magicToUseWhenAttack = magicToUse;

                var value = Globals.TheRandom.Next(3);
                if (value == 1 && NpcIni.ContainsKey((int)CharacterState.Attack1))
                    SetState(CharacterState.Attack1);
                else if (value == 2 && NpcIni.ContainsKey((int)CharacterState.Attack2))
                    SetState(CharacterState.Attack2);
                else SetState(CharacterState.Attack);

                OnPerformeAttack();

                if (magicToUse.UseActionFile != null)
                {
                    Texture = magicToUse.UseActionFile;
                }

                SetDirection(destinationPositionInWorld - PositionInWorld);
                PlayCurrentDirOnce();
            }
        }

        public void ToNonFightingState()
        {
            _isInFighting = false;
            if (IsWalking()) SetState(CharacterState.Walk);
            if (IsRuning()) SetState(CharacterState.Run);
            if (State == (int)CharacterState.FightStand) SetState(CharacterState.Stand);
        }

        public void ToFightingState()
        {
            _isInFighting = true;
            _totalNonFightingSeconds = 0;
        }

        public void InteractWith(object target, bool isRun = false)
        {
            if (PerformActionOk())
            {
                _interactiveTarget = target;
                _isRunToTarget = isRun;

                Vector2 destinationTilePositon;
                int interactDistance;
                if (!GetInteractTargetInfo(out destinationTilePositon, out interactDistance))
                {
                    return;
                }
                var destinationPositionInWorld = MapBase.ToPixelPosition(destinationTilePositon);

                DestinationMoveTilePosition = Engine.PathFinder.FindDistanceTileInDirection(
                    destinationTilePositon,
                    PositionInWorld - destinationPositionInWorld,
                    interactDistance);
                //if can't reach destination tile positon, find neighbor tile
                if (MapBase.Instance.IsObstacleForCharacter(DestinationMoveTilePosition) ||
                    HasObstacle(DestinationMoveTilePosition))
                {
                    //Try all 8 direction
                    var directionList = Utils.GetDirection8List();
                    var isFinded = false;
                    foreach (var dir in directionList)
                    {
                        DestinationMoveTilePosition = Engine.PathFinder.FindDistanceTileInDirection(
                            destinationTilePositon,
                            dir,
                            interactDistance);
                        if (!MapBase.Instance.IsObstacleForCharacter(DestinationMoveTilePosition) &&
                            !HasObstacle(DestinationMoveTilePosition))
                        {
                            isFinded = true;
                            break;
                        }
                    }
                    if (!isFinded)
                    {
                        //Not find, can't move to target, do nothing.
                        _interactiveTarget = null;
                        return;
                    }
                }

                if (IsStanding() && InteractIsOk())
                    PerformeInteract();
            }
        }

        private bool GetInteractTargetInfo(out Vector2 tilePosition, out int interactDistance)
        {
            tilePosition = Vector2.Zero;
            interactDistance = 0;
            if (_interactiveTarget == null) return false;
            var character = _interactiveTarget as Character;
            var obj = _interactiveTarget as Obj;
            if (character != null)
            {
                tilePosition = character.TilePosition;
                interactDistance = character.DialogRadius;
            }
            else if (obj != null)
            {
                tilePosition = obj.TilePosition;
                interactDistance = 1;
            }
            else
                return false;
            return true;
        }

        private bool InteractIsOk()
        {
            if (_interactiveTarget == null) return false;
            Vector2 destinationTilePositon;
            int interactDistance;
            if (!GetInteractTargetInfo(out destinationTilePositon, out interactDistance))
            {
                return false;
            }

            var tileDistance = Engine.PathFinder.GetViewTileDistance(TilePosition, destinationTilePositon);
            if (tileDistance <= interactDistance)
            {
                return true;
            }
            else
            {
                MoveToTarget(DestinationMoveTilePosition, _isRunToTarget);
                return false;
            }
        }

        private void PerformeInteract()
        {
            if (PerformActionOk())
            {
                var character = _interactiveTarget as Character;
                var obj = _interactiveTarget as Obj;
                if (character != null)
                {
                    character.StartInteract(this);
                    SetDirection(character.PositionInWorld - PositionInWorld);
                }
                else if (obj != null)
                {
                    obj.StartInteract();
                    SetDirection(obj.PositionInWorld - PositionInWorld);
                }
                StandingImmediately();
            }
        }

        public void StartInteract(Character from)
        {
            if (from != null && !string.IsNullOrEmpty(ScriptFile))
            {
                _isInInteract = true;
                _directionBeforInteract = CurrentDirection;
                SetDirection(from.PositionInWorld - PositionInWorld);
                _currentRunInteractScript = ScriptManager.RunScript(Utils.GetScriptParser(ScriptFile), this);
            }
        }

        public bool IsInteractEnd()
        {
            if (_currentRunInteractScript != null)
            {
                return _currentRunInteractScript.IsEnd;
            }
            return true;
        }

        /// <summary>
        /// Walk or run to destination.
        /// If distance greater than 5, run to destination.
        /// If distance greater than 2, and is running, run to destination, else walk to destination.
        /// </summary>
        /// <param name="destinationTilePosition">Destination tile position</param>
        public void PartnerMoveTo(Vector2 destinationTilePosition)
        {
            if (MapBase.Instance.IsObstacleForCharacter(destinationTilePosition))
            {
                //Destination is obstacle, can't move to destination, do nothing.
                return;
            }

            var distance = Engine.PathFinder.GetViewTileDistance(TilePosition,
                destinationTilePosition);
            if (distance > 20)
            {
                Globals.ThePlayer.ResetPartnerPosition();
            }
            else if (distance > 5)
            {
                RunTo(destinationTilePosition);
            }
            else if (distance > 2)
            {
                if (IsRuning())
                {
                    RunTo(destinationTilePosition);
                }
                else
                {
                    WalkTo(destinationTilePosition);
                }
            }
        }

        #endregion Perform action

        #region Character state set and get method

        /// <summary>
        /// Set character state.Change texture and play sound.
        /// </summary>
        /// <param name="state">State to set</param>
        /// <param name="setIfStateSame">If true renew state if current state is same as the setting state</param>
        /// <param name="setNullTexturn">Set texture to null is state not exist.Otherwise do nothing.</param>
        public void SetState(CharacterState state, bool setIfStateSame = false, bool setNullTexturn = false)
        {
            if ((State != (int)state || setIfStateSame) &&
                NpcIni.ContainsKey((int)state))
            {
                if (_sound != null)
                {
                    _sound.Stop(true);
                    _sound = null;
                }

                var image = NpcIni[(int)state].Image;
                var sound = NpcIni[(int)state].Sound;
                Texture = image;
                if (sound != null)
                {
                    switch (state)
                    {
                        case CharacterState.Walk:
                        case CharacterState.FightWalk:
                        case CharacterState.Run:
                        case CharacterState.FightRun:
                            {
                                _sound = sound.CreateInstance();
                                _sound.IsLooped = true;
                                SoundManager.Apply3D(_sound,
                                    PositionInWorld - Globals.ListenerPosition);
                                _sound.Play();
                            }
                            break;
                        case CharacterState.Magic:
                        case CharacterState.Attack:
                        case CharacterState.Attack1:
                        case CharacterState.Attack2:
                            {
                                //do nothing
                            }
                            break;
                        default:
                            PlaySoundEffect(sound);
                            break;
                    }
                }
                State = (int)state;
            }
            else
            {
                if (setNullTexturn)
                {
                    Texture = null;
                }
            }
        }

        /// <summary>
        /// Clear frozen, poison, petrifaction
        /// </summary>
        public void ToNormalState()
        {
            ClearFrozen();
            ClearPoison();
            ClearPetrifaction();
        }

        public void ClearFrozen()
        {
            _frozenSeconds = 0;
        }

        public void ClearPoison()
        {
            _poisonSeconds = 0;
        }

        public void ClearPetrifaction()
        {
            _petrifiedSeconds = 0;
        }

        public static float GetTrueDistance(Vector2 distance)
        {
            var unit = Utils.GetDirection8(Utils.GetDirectionIndex(distance, 8));
            distance = (2f - Math.Abs(unit.X)) * distance;
            return distance.Length();
        }

        /// <summary>
        /// Add amount of life. Amount can be negative.
        /// If list is less than 0.Character is death and <see cref="Death"/> is invoked.
        /// </summary>
        /// <param name="amount">Amount to add. If amount is nagetive, life is decreased.</param>
        public void AddLife(int amount)
        {
            Life += amount;
            if (Life <= 0) Death();
        }

        /// <summary>
        /// Decrease amount of life. If life is greater than 0, <see cref="Hurting"/> is invoked.
        /// Amount must be positive, oherwise no effect.
        /// </summary>
        /// <param name="amount">Amount must be positive, oherwise no effect.</param>
        public void DecreaseLifeAddHurt(int amount)
        {
            if (amount <= 0) return;
            AddLife(-amount);
            if (Life > 0)
            {
                Hurting();
            }
        }

        public void AddMana(int amount)
        {
            Mana += amount;
        }

        public void AddThew(int amount)
        {
            Thew += amount;
        }

        /// <summary>
        /// Do nothing, override this method to add behaviour
        /// </summary>
        /// <param name="magicFileName"></param>
        public virtual void AddMagic(string magicFileName) { }

        /// <summary>
        /// Level up to level
        /// </summary>
        /// <param name="level">The level</param>
        public virtual void SetLevelTo(int level)
        {
            Level = level;
            if (LevelIni == null)
            {
                return;
            }
            Utils.LevelDetail detail = null;
            if (LevelIni.ContainsKey(level))
            {
                detail = LevelIni[level];
            }
            if (detail != null)
            {
                LifeMax = detail.Life;
                ThewMax = detail.ThewMax;
                ManaMax = detail.ManaMax;
                Life = LifeMax;
                Thew = ThewMax;
                Mana = ManaMax;
                Attack = detail.Attack;
                Attack2 = detail.Attack2;
                Defend = detail.Defend;
                Defend2 = detail.Defend2;
                Evade = detail.Evade;
                LevelUpExp = detail.LevelUpExp;
            }
        }

        public virtual void FullLife()
        {
            Life = LifeMax;
            IsDeath = false;
            IsDeathInvoked = false;
        }

        public void FullThew()
        {
            Thew = ThewMax;
        }

        public void FullMana()
        {
            Mana = ManaMax;
        }

        public void SetKind(int kind)
        {
            Kind = kind;
        }

        /// <summary>
        /// Set FlyIni
        /// </summary>
        /// <param name="fileName">Magic file name</param>
        public virtual void SetMagicFile(string fileName)
        {
            FlyIni = Utils.GetMagic(fileName);
        }

        public virtual void SetTilePosition(Vector2 tilePosition)
        {
            TilePosition = tilePosition;
        }

        public virtual void SetPosition(Vector2 tilePosition)
        {
            //Stand when reposition
            StandingImmediately();
            TilePosition = tilePosition;
        }

        public virtual void SetRelation(int relation)
        {

            if (
                (_relation == (int)RelationType.Friend &&
                relation == (int)RelationType.Enemy) ||
                (_relation == (int)RelationType.Enemy &&
                 relation != (int)RelationType.Enemy)
                )
            {
                //Character can't follow current target now
                FollowTarget = null;
            }
            _relation = relation;
        }

        /// <summary>
        /// Set NpcIni flle
        /// </summary>
        /// <param name="fileName"></param>
        public virtual void SetNpcIni(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            _npcIniFileName = fileName;
            NpcIni = ResFile.ReadFile(@"ini\npcres\" + fileName, ResType.Npc);
        }

        /// <summary>
        /// Set NpcIni flle and refresh draw image
        /// </summary>
        /// <param name="fileName"></param>
        public void SetRes(string fileName)
        {
            SetNpcIni(fileName);
            if (NpcIni != null && NpcIni.Count > 0)
            {
                if (NpcIni.ContainsKey(State))
                {
                    SetState((CharacterState)State, true);
                }
                else
                {
                    foreach (var key in NpcIni.Keys)
                    {
                        SetState((CharacterState)key);
                        break;
                    }
                }
            }
            else
            {
                Texture = null;
            }
        }

        /// <summary>
        /// Set state image and refresh draw
        /// </summary>
        /// <param name="state">State to set</param>
        /// <param name="fileName">Asf file name</param>
        public void SetNpcActionFile(CharacterState state, string fileName)
        {
            ResFile.SetNpcStateImage(NpcIni, state, fileName);
            SetState((CharacterState)State, true);
        }

        public void SetNpcActionType(int type)
        {
            Action = type;
        }

        public void DisableFight()
        {
            IsFightDisabled = true;
        }

        public void EnableFight()
        {
            IsFightDisabled = false;
        }

        public void DisableJump()
        {
            IsJumpDisabled = true;
        }

        public void EnableJump()
        {
            IsJumpDisabled = false;
        }

        public void DisableRun()
        {
            IsRunDisabled = true;
        }

        public void EnableRun()
        {
            IsRunDisabled = false;
        }

        public void SetFightState(bool isFight)
        {
            if (isFight)
            {
                ToFightingState();
                SetState(CharacterState.FightStand);
            }
            else
            {
                ToNonFightingState();
                SetState(CharacterState.Stand);
            }
        }

        /// <summary>
        /// Make this enemy and all neighbor enemy walk to target and follow target 
        /// If follow target is already finded and distance is less than new target,
        /// don't walk to and follow new target
        /// </summary>
        /// <param name="target">The target</param>
        public void NotifyEnemyAndAllNeighbor(Character target)
        {
            if (target == null || !IsEnemy || !IsStanding()) return;
            var characters = NpcManager.GetNeighborEnemy(this);
            characters.Add(this);
            foreach (var character in characters)
            {
                if (character.FollowTarget != null &&
                    character.IsFollowTargetFound &&
                    Vector2.Distance(character.PositionInWorld, character.FollowTarget.PositionInWorld) <
                    Vector2.Distance(character.PositionInWorld, target.PositionInWorld))
                {
                    continue;
                }
                character.FollowAndWalkToTarget(target);
            }
        }

        /// <summary>
        /// Walk to target and follow target
        /// </summary>
        /// <param name="target">Target to walk to</param>
        public void FollowAndWalkToTarget(Character target)
        {
            WalkTo(target.TilePosition);
            Follow(target);
        }

        /// <summary>
        /// Set follow target
        /// </summary>
        /// <param name="target">Follow target</param>
        public void Follow(Character target)
        {
            FollowTarget = target;
            IsFollowTargetFound = true;
        }

        public void SetSpecialAction(string asfFileName)
        {
            IsInSpecialAction = true;
            _specialActionLastDirection = CurrentDirection;
            EndPlayCurrentDirOnce();
            Texture = Utils.GetCharacterAsf(asfFileName);
            PlayCurrentDirOnce();
        }

        public void ClearFollowTarget()
        {
            FollowTarget = null;
        }

        public void ShowRangeRadius(int radius)
        {
            _rangeRadiusToShow = radius;
        }
        #endregion Character state set and get method

        #region Update Draw

        private ScriptParser _timeScriptParserCache;
        public override void Update(GameTime gameTime)
        {
            if (!IsDeathInvoked && !IsDeath)
            {
                if (!string.IsNullOrEmpty(_timerScriptFile))
                {
                    _timerScriptIntervlElapsed += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (_timerScriptIntervlElapsed >= _timerScriptInterval)
                    {
                        _timerScriptIntervlElapsed -= _timerScriptInterval;
                        if (_timeScriptParserCache == null ||
                            Globals.TheGame.IsInEditMode // Turn off cache script in edit mode
                            )
                        {
                            _timeScriptParserCache = Utils.GetScriptParser(_timerScriptFile);
                        }
                        ScriptManager.RunScript(_timeScriptParserCache, this);
                    }
                }
            }

            if (SppedUpByMagicSprite != null)
            {
                if (SppedUpByMagicSprite.IsInDestroy || SppedUpByMagicSprite.IsDestroyed)
                {
                    SppedUpByMagicSprite = null;
                }
                else
                {
                    var fold = (100 + SppedUpByMagicSprite.BelongMagic.RangeSpeedUp) / 100f;
                    gameTime = new GameTime(new TimeSpan((long)(gameTime.TotalGameTime.Ticks * fold)), 
                        new TimeSpan((long)(gameTime.ElapsedGameTime.Ticks * fold)), 
                        gameTime.IsRunningSlowly);

                }
            }
            
            for (var node = _summonedNpcs.First; node != null;)
            {
                var next = node.Next;
                var npc = node.Value;
                if (npc.IsDeath)
                {
                    _summonedNpcs.Remove(node);
                }
                node = next;
            }

            if (IsInSpecialAction)
            {
                base.Update(gameTime);
                if (IsPlayCurrentDirOnceEnd())
                {
                    IsInSpecialAction = false;
                    EndSpecialAction();
                    //Restore direction
                    SetDirectionValue(_specialActionLastDirection);
                }
                return;
            }

            if (IsDeath) return;

            if (_isInInteract && IsInteractEnd())
            {
                EndInteract();
            }

            var elapsedGameTime = gameTime.ElapsedGameTime;

            if (PoisonSeconds > 0)
            {
                PoisonSeconds -= (float)elapsedGameTime.TotalSeconds;
                _poisonedMilliSeconds += (float)elapsedGameTime.TotalMilliseconds;
                if (_poisonedMilliSeconds > 250)
                {
                    _poisonedMilliSeconds = 0;
                    AddLife(-10);
                }
            }

            if (PetrifiedSeconds > 0)
            {
                PetrifiedSeconds -= (float)elapsedGameTime.TotalSeconds;
                return;
            }

            if (FrozenSeconds > 0)
            {
                FrozenSeconds -= (float)elapsedGameTime.TotalSeconds;
                elapsedGameTime = new TimeSpan(elapsedGameTime.Ticks / 2);
                var totalGameTime = new TimeSpan(gameTime.TotalGameTime.Ticks/2);
                gameTime = new GameTime(totalGameTime, elapsedGameTime);
            }

            switch ((CharacterState)State)
            {
                case CharacterState.Walk:
                case CharacterState.FightWalk:
                    {
                        if (_fixedPathMoveDestinationPixelPostion != Vector2.Zero)
                        {
                            var direction = _fixedPathMoveDestinationPixelPostion - PositionInWorld;
                            MoveTo(direction,
                                (float)elapsedGameTime.TotalSeconds * WalkSpeed * 2);
                            if (direction == Vector2.Zero ||
                                MovedDistance >= _fixedPathDistanceToMove)
                            {
                                StandingImmediately();
                                _fixedPathMoveDestinationPixelPostion = Vector2.Zero;
                            }
                        }
                        else
                        {
                            MoveAlongPath((float)elapsedGameTime.TotalSeconds, WalkSpeed);
                        }
                        SoundManager.Apply3D(_sound,
                                    PositionInWorld - Globals.ListenerPosition);
                        Update(gameTime, WalkSpeed);
                    }
                    break;
                case CharacterState.Run:
                case CharacterState.FightRun:
                    MoveAlongPath((float)elapsedGameTime.TotalSeconds, Globals.RunSpeedFold);
                    SoundManager.Apply3D(_sound,
                                    PositionInWorld - Globals.ListenerPosition);
                    base.Update(gameTime);
                    break;
                case CharacterState.Jump:
                case CharacterState.FightJump:
                    JumpAlongPath((float)elapsedGameTime.TotalSeconds);
                    base.Update(gameTime);
                    break;
                case CharacterState.Stand:
                case CharacterState.Stand1:
                case CharacterState.Hurt:
                    base.Update(gameTime);
                    if (IsPlayCurrentDirOnceEnd()) StandingImmediately();
                    break;
                case CharacterState.Attack:
                case CharacterState.Attack1:
                case CharacterState.Attack2:
                    base.Update(gameTime);
                    if (IsPlayCurrentDirOnceEnd())
                    {
                        if (NpcIni.ContainsKey(State))
                        {
                            PlaySoundEffect(NpcIni[State].Sound);
                        }
                        if (_magicToUseWhenAttack != null)
                        {
                            MagicManager.UseMagic(this,
                           _magicToUseWhenAttack,
                           PositionInWorld,
                           _attackDestination);
                        }
                       

                        //Do somethig when attacking
                        OnAttacking(_attackDestination);

                        StandingImmediately();
                    }
                    break;
                case CharacterState.Magic:
                    base.Update(gameTime);
                    if (IsPlayCurrentDirOnceEnd())
                    {
                        StandingImmediately();
                        if (NpcIni.ContainsKey((int)CharacterState.Magic))
                        {
                            PlaySoundEffect(NpcIni[(int)CharacterState.Magic].Sound);
                        }
                        if (CanUseMagic())
                        {
                            MagicManager.UseMagic(this, MagicUse, PositionInWorld, _magicDestination, _magicTarget);
                            MagicUsedHook(MagicUse);
                        }
                    }
                    break;
                case CharacterState.Sit:
                    if (!IsSitted) base.Update(gameTime);
                    if (!IsInPlaying) IsSitted = true;
                    break;
                case CharacterState.Death:
                    base.Update(gameTime);
                    if (IsPlayCurrentDirOnceEnd())
                    {
                        IsDeath = true;
                    }
                    break;
                default:
                    base.Update(gameTime);
                    break;
            }
            if (_isInFighting)
            {
                _totalNonFightingSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_totalNonFightingSeconds > MaxNonFightSeconds)
                {
                    ToNonFightingState();
                }
            }

            for (var node = MagicSpritesInEffect.First; node != null; )
            {
                var next = node.Next;
                var magicSprite = node.Value;
                if (magicSprite.IsDestroyed)
                    MagicSpritesInEffect.Remove(node);
                node = next;
            }

            if (MovedByMagicSprite != null)
            {
                if (MovedByMagicSprite.IsInDestroy || MovedByMagicSprite.IsDestroyed)
                {
                    MovedByMagicSprite = null;
                }
                else
                {
                    if (Engine.PathFinder.CanLinearlyMove(this, TilePosition, MovedByMagicSprite.TilePosition))
                    {
                        PositionInWorld = MovedByMagicSprite.PositionInWorld;
                        SetDirection(MovedByMagicSprite.MoveDirection);
                    }
                    else
                    {
                        MovedByMagicSprite = null;
                    }
                }
            }

            const float friction = 4;
            if (BouncedVelocity > 0)
            {
                var newPosition = PositionInWorld + BouncedDirection * (BouncedVelocity * (float)elapsedGameTime.TotalSeconds);
                var newTilePosition = MapBase.ToTilePosition(newPosition);
                if (Engine.PathFinder.CanLinearlyMove(this, TilePosition, newTilePosition))
                {
                    PositionInWorld = newPosition;
                    BouncedVelocity -= friction;
                    if (BouncedVelocity <= 0)
                    {
                        BouncedVelocity = 0;
                    }
                }
                else
                {
                    BouncedVelocity = 0;
                }
            }
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            Draw(spriteBatch, GetCurrentTexture());
        }


        private Color _radiusTileColorCached = Color.Black;
        private Vector2 _radiusRangeLastTilePosition;
        private LinkedList<Vector2> _lastRadiusTileTilesCached;
        public virtual void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            if (!(IsDeath || IsHide || IsInTransport))
            {
                var color = DrawColor;
                if (FrozenSeconds > 0 && _isFronzenVisualEffect)
                    color = new Color(80, 80, 255);
                if (PoisonSeconds > 0 && _isPoisionVisualEffect)
                    color = new Color(50, 255, 50);
                if (PetrifiedSeconds > 0 && _isPetrifiedVisualEffect)
                {
                    spriteBatch.End();
                    JxqyGame.BeginSpriteBatch(spriteBatch, Globals.TheGame.GrayScaleEffect);
                }
                base.Draw(spriteBatch, texture, color);
                if (PetrifiedSeconds > 0 && _isPetrifiedVisualEffect)
                {
                    spriteBatch.End();
                    JxqyGame.BeginSpriteBatch(spriteBatch);
                }
            }

            DrawRangeRadius(spriteBatch);
        }

        public void DrawRangeRadius(SpriteBatch spriteBatch)
        {
            if (_rangeRadiusToShow > 0)
            {
                if (_radiusRangeLastTilePosition != TilePosition || _lastRadiusTileTilesCached == null)
                {
                    _lastRadiusTileTilesCached = Engine.PathFinder.FindAllNeighborsInRadius(
                        TilePosition,
                        _rangeRadiusToShow);
                }

                if (_radiusTileColorCached == Color.Black)
                {
                    _radiusTileColorCached = new Color(
                        Globals.TheRandom.Next(80, 250),
                        Globals.TheRandom.Next(0, 200),
                        Globals.TheRandom.Next(70, 200),
                        180);
                }

                const int width = 10;
                const int height = 10;
                foreach (var tilePositon in _lastRadiusTileTilesCached)
                {
                    var pixelPosition = MapBase.ToPixelPosition(tilePositon);
                    var worldRegion = new Rectangle((int)pixelPosition.X - width / 2,
                        (int)pixelPosition.Y - height / 2,
                        width,
                        height);
                    var screenRegion = Globals.TheCarmera.ToViewRegion(worldRegion);
                    InfoDrawer.DrawRectangle(spriteBatch,
                        screenRegion,
                        tilePositon == TilePosition ? Color.Gold : _radiusTileColorCached);
                }
            }
        }
        #endregion Update Draw

        #region Type
        public enum CharacterKind
        {
            Normal,
            Fighter,
            Player,
            Follower,
            GroundAnimal,
            Eventer,
            AfraidPlayerAnimal,
            Flyer
        }

        public enum RelationType
        {
            Friend,
            Enemy,
            Neutral
        }

        public enum ActionType
        {
            Stand,
            RandWalk,
            LoopWalk
        }

        public enum BeAttackedUseMagicDirection
        {
            Attacker,
            MagicSpriteOppDirection,
            CurrentNpcDirection,
        }

        public enum DeathUseMagicDirection
        {
            LastAttacker,
            LastMagicSpriteOppDirection,
            CurrentNpcDirection,
        }
        #endregion Type
    }
}
