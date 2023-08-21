using System.Collections.Generic;
using UnityEngine;
using LionStudios;
using System;
using System.Threading.Tasks;

    public partial class WorldData : SerializableContainer
    {
        public interface IPositionedObject : IPersistentDataRemote
        {
            Vector2Int GetPosition();
            void SetPosition(Vector2Int position);
            bool CheckEqualGeneric(IPositionedObject obj);

            ToolType Type();
        }

        public static bool CheckEquality<T>(List<T> a, List<T> b) where T : IPositionedObject
        {
            if (a.Count != b.Count)
            {
                Debug.Log(typeof(T) + " :: Count changed");
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].CheckEqualGeneric(b[i]) == false)
                {
                    Debug.Log(typeof(T) + " :: Value changed at index '" + i + "'");
                    return false;
                }
            }

            return true;
        }

        [Serializable]
        public class PositionedObject<T> : PersistentDataRemote<T>, IPositionedObject where T : PositionedObject<T>, new()
        {
            public Vector2Int position;
            public int islandIndex = -1;

            public PositionedObject() { }
            public PositionedObject(int x, int y, ToolType type)
            {
                instanceId = Guid.NewGuid().ToString();
                position = new Vector2Int(x, y);
                _ToolType = type;
            }

            [SerializeField] protected ToolType _ToolType = ToolType.MissingTool;

            public ToolType Type()
            {
                if (_ToolType == ToolType.MissingTool)
                {
                    if (this is Harvester)
                    {
                        Harvester harvester = this as Harvester;
                        switch(harvester.variantIndex)
                        {
                            case 0:
                                _ToolType = ToolType.Harvester0;
                                break;
                            case 1:
                                _ToolType = ToolType.Harvester1;
                                break;
                            case 2:
                                _ToolType = ToolType.Harvester2;
                                break;
                            case 3:
                                _ToolType = ToolType.Harvester3;
                                break;
                            case 4:
                                _ToolType = ToolType.Harvester4;
                                break;
                            case 5:
                                _ToolType = ToolType.Harvester5;
                                break;
                            case 6:
                                _ToolType = ToolType.Harvester6;
                                break;
                            case 7:
                                _ToolType = ToolType.Harvester7;
                                break;
                            case 8:
                                _ToolType = ToolType.Harvester8;
                                break;
                        }
                        Debug.Log("Harvester Var " + harvester.variantIndex);

                        if (_ToolType == ToolType.MissingTool)
                            Debug.LogError("Harvester variant not found");
                    }

                    if (_ToolType == ToolType.MissingTool)
                        Debug.LogError("Tool type was never set.");
                }

                return _ToolType;
            }

            public void SetType(ToolType type)
            {
                _ToolType = type;
            }

            Vector2Int IPositionedObject.GetPosition()
            {
                return position;
            }

            void IPositionedObject.SetPosition(Vector2Int position)
            {
                this.position = position;
            }

            public override void SaveLocal()
            {
                base.SaveLocal();
            }

            public override void EnqueueSaveRemote()
            {
                // Only save if we have unlocked the expansion that this object is part of
                if (ExpansionController.IsInUnlockedExpansion(position))
                {
                    base.EnqueueSaveRemote();
                }
            }

            public override bool CheckEqual(T inst)
            {
#if UNITY_EDITOR
                if (base.CheckEqual(inst) == false)
                    return false;

                if (position != inst.position)
                {
                    Debug.Log(this + " :: Pos Changed :: " + position + " != " + inst.position);
                    return false;
                }

                if (_ToolType != inst._ToolType)
                {
                    Debug.Log(this + " :: _ToolType Changed :: " + _ToolType + " != " + inst._ToolType);
                    return false;
                }

                return true;
#else
                return  base.CheckEqual(inst) &&
                        position == inst.position &&
                        _ToolType == inst._ToolType;
#endif
            }

            public bool CheckEqualGeneric(IPositionedObject obj)
            {
                return CheckEqual(obj as T);
            }

            public async Task Verify()
            {
                return;
                T localVersion = Load(instanceId);

                if (localVersion.instanceId != instanceId)
                {
                    Debug.LogError(this + " :: Local InstId = " + instanceId + " != " + localVersion.instanceId);
                }

                if (localVersion.position != position)
                {
                    Debug.LogError(this + " :: Local Position " + position + " != " + localVersion.position);
                }

                if (localVersion._ToolType != _ToolType)
                {
                    Debug.LogError(this + " :: Local Type " + _ToolType + " != " + localVersion._ToolType);
                }

                if (localVersion.islandIndex != islandIndex)
                {
                    Debug.LogError(this + " :: Local IslandIndex " + islandIndex + " != " + localVersion.islandIndex);
                }

                T remoteVersion = await ReloadRemoteAsync();

                if (remoteVersion.instanceId != instanceId)
                {
                    Debug.LogError(this + " :: Remote InstId = " + instanceId + " != " + remoteVersion.instanceId);
                }

                if (remoteVersion.position != position)
                {
                    Debug.LogError(this + " :: Remote Position " + position + " != " + remoteVersion.position);
                }

                if (remoteVersion._ToolType != _ToolType)
                {
                    Debug.LogError(this + " :: Remote Type " + _ToolType + " != " + remoteVersion._ToolType);
                }

                if (remoteVersion.islandIndex != islandIndex)
                {
                    Debug.LogError(this + " :: Remote IslandIndex " + islandIndex + " != " + remoteVersion.islandIndex);
                }

                Debug.Log("Verified  : " + this);
            }
        }

        [Serializable]
        public class Tile : PositionedObject<Tile>//, IPersistentDataRemote
        {
            public Tile() { }
            public Tile(int x, int y, ToolType type) : base(x, y, type)
            {
            }

        }

        [Serializable]
        public class Planter : PositionedObject<Planter>
        {
            public Planter() : base() { }
            public Planter(int x, int y, ToolType type) : base(x, y, type)
            {
            }

            public Plant plant;

            public override bool CheckEqual(Planter inst)
            {
                return base.CheckEqual(inst) && plant.CheckEqual(inst.plant);

            }
        }

        [Serializable]
        public class Plant
        {
            public string plantName;
            public long millisGrown;
            public long millisOvergrown;
            public long lastSaveTime;

            public int level = 1;

            public Plant(string plantName)
            {
                this.plantName = plantName;
            }

            public bool CheckEqual(Plant inst)
            {
                return plantName == inst.plantName;
            }
        }

        [Serializable]
        public class House : PositionedObject<House>
        {
            public House() : base() { }
            public House(int x, int y, ToolType type, int rotations) : base(x, y, type)
            {
                this.rotations = rotations;
            }

            public int rotations;
            public List<Harvester> harvesters = new List<Harvester>();

            public int GetSize ()
            {
                return WorldController.GetSizeForHouseType(_ToolType);
            }

            public override bool CheckEqual(House inst)
            {
                return base.CheckEqual(inst) &&
                    rotations == inst.rotations &&
                    CheckEquality(harvesters, inst.harvesters);
            }
        }

        [Serializable]
        public class Hologram
        {
            public enum HologramType
            {
                House,
                Totem,
                Decor
            }

            public HologramType hologramType;
            public Vector2Int position;
            public int rotations;

            public Hologram(int x, int y, HologramType hologramType, int rotations)
            {
                this.hologramType = hologramType;
                this.position = new Vector2Int(x, y);
                this.rotations = rotations;
            }
        }

        [Serializable]
        public class Harvester : PositionedObject<Harvester>
        {
            public Vector2Int direction = Vector2Int.down;
            public int variantIndex = 0;
            public string name = "Gardener";

            public Harvester()
            {
                instanceId = Guid.NewGuid().ToString();
            }
            public Harvester(int x, int y, int dx, int dy, int variantIndex, ToolType type) : base(x, y, type)
            {
                direction = new Vector2Int(dx, dy);
                this.variantIndex = variantIndex;
            }

            public override bool CheckEqual(Harvester inst)
            {
                return base.CheckEqual(inst) &&
                        variantIndex == inst.variantIndex &&
                        direction == inst.direction &&
                        name == inst.name;
            }
        }

        [Serializable]
        public class Pet : PositionedObject<Pet>
        {
            public Pet() { }
            public Pet(int x, int y, int dx, int dy, ToolType type) : base(x, y, type)
            {
                direction = new Vector2Int(dx, dy);
            }

            public Vector2Int direction = Vector2Int.down;

            public override bool CheckEqual(Pet inst)
            {
                return base.CheckEqual(inst) &&
                        direction == inst.direction;
            }
        }



        [Serializable]
        public class Decor : PositionedObject<Decor>
        {
            public int rotations;
            public Decor() {}
            public Decor(int x, int y, ToolType type, int rotations = 0) : base(x, y, type)
            {
                this.rotations = rotations;
            }

            public override bool CheckEqual(Decor inst)
            {
                return base.CheckEqual(inst) && rotations == inst.rotations;
            }
        }

        [Serializable]
        public class PointOfInterest : PositionedObject<PointOfInterest>
        {
            public PointOfInterest() { }
            public PointOfInterest(int x, int y) : base(x, y, ToolType.PointOfInterest)
            {
            }
        }

        [Serializable]
        public class Misc : PositionedObject<Misc>
        {
            public string truncDescription;
            public List<Vector2Int> areaOfEffect;

            public Misc() { }
            public Misc(int x, int y, ToolType type, string desc, List<Vector2Int> areaOfEffectSet) : base(x, y, type)
            {
                truncDescription = desc;
                areaOfEffect = areaOfEffectSet;
            }

            public bool CheckAOEEquality(Misc inst)
            {
                if (areaOfEffect.Count != inst.areaOfEffect.Count)
                    return false;

                for (int i = 0; i < areaOfEffect.Count; i++)
                {
                    if (areaOfEffect[i] != inst.areaOfEffect[i])
                        return false;
                }

                return true;
            }

            public override bool CheckEqual(Misc inst)
            {
                return base.CheckEqual(inst) &&
                    truncDescription == inst.truncDescription &&
                    CheckAOEEquality(inst);
            }
        }

        [Serializable]
        public class Totem : PositionedObject<Totem>
        {
            public int rotations;

            public Totem() { }
            public Totem(int x, int y, ToolType type, int rotations) : base(x, y, type)
            {
                this.rotations = rotations;
            }

            public override bool CheckEqual(Totem inst)
            {
                return base.CheckEqual(inst) && rotations == inst.rotations;
            }
        }

        [Serializable]
        public class StorageBuilding : PositionedObject<StorageBuilding>
        {
            public StorageBuilding() { }
            public StorageBuilding(int x, int y, ToolType type) : base(x, y, type) {}
        }

        [Serializable]
        public class CraftingBuilding : PositionedObject<CraftingBuilding>
        {
            public const int CRAFTING_BUILDING_STARTING_QUEUE_SIZE = 2;
            public int queueSize = CRAFTING_BUILDING_STARTING_QUEUE_SIZE;
            public int rotations;

            public CraftingBuilding() { }
            public CraftingBuilding(int x, int y, ToolType type, int rotations) : base(x, y, type)
            {
                this.rotations = rotations;
            }

            public override bool CheckEqual(CraftingBuilding inst)
            {
                return base.CheckEqual(inst) &&
                    rotations == inst.rotations &&
                    queueSize == inst.queueSize;
            }
        }

        [Serializable]
        public class OrderboardBuildingPO : PositionedObject<OrderboardBuildingPO>
        {
            public OrderboardBuildingPO() { }
            public OrderboardBuildingPO(int x, int y, ToolType type, int rotations) : base(x, y, type)
            {
                this.rotations = rotations;
            }

            public int rotations;

            public override bool CheckEqual(OrderboardBuildingPO inst)
            {
                return base.CheckEqual(inst) && rotations == inst.rotations;
            }
        }

        [Serializable]
        public class InventoryReward
        {
            public ToolType itemId;
            public int quantity;
        }

        [Serializable]
        public class ChestPO : PositionedObject<ChestPO>
        {
            public int coinReward;
            public int gemReward;

            [SerializeField]
            public ChestState currentState;

            public List<Requirement> storedItemRewards = new List<Requirement>();
            public List<InventoryReward> inventoryRewards = new List<InventoryReward>();

            public ChestPO() { _ToolType = ToolType.CommonChest; }
            public ChestPO(int x, int y, ToolType type, int rotations) : base(x, y, type)
            {
                //this.chestType = type;
                this.rotations = rotations;
            }

            public int rotations;

            public override bool CheckEqual(ChestPO inst)
            {
                return base.CheckEqual(inst) && rotations == inst.rotations;
            }

            public void UpdateChestState(ChestState state, Vector2Int position)
            {
                currentState = state;

                WorldController.Instance.DeleteChest(position);
            }
        }

        [Serializable]
        public class Goal : PersistentDataRemote<Goal>
        {
            public int progress;
            public bool collected;
        }

        [Serializable]
        public class Craft : PersistentDataRemote<Craft>
        {
            public long dateTimeBegun;
            public long dateTimeQueued;
            public bool isComplete;
            public string itemId;

            public Craft()
            {
                instanceId = Guid.NewGuid().ToString();
            }
            public Craft(string itemIdSet, long dateTimeBegunSet, long dateTimeQueuedSet, bool isCompleteSet)
            {
                instanceId = Guid.NewGuid().ToString();
                itemId = itemIdSet;
                dateTimeBegun = dateTimeBegunSet;
                dateTimeQueued = dateTimeQueuedSet;
                isComplete = isCompleteSet;
            }
        }

        [Serializable]
        public class Player : PersistentDataRemote<Player>
        {
            public static long LastSaveAttempt;

            public double money = 800.0f;
            public double aggregateMoney;
            public int gems = 1;
            public int aggregateGems = 1;
            public int standardInstaGrowTokens;
            public int premiumInstaGrowTokens;
            public int level = 1;
            public double xp;
            public string deviceId;
            public string nakamaUserId;
            public long saveTime;
            public float currentLevelActiveTime;
            public bool firstTimeLaunch = true;
            public List<string> unlockedExpansions;
            public string clientVersion = "0.0.0";

            public override void SaveLocal()
            {
                if (Database.Paused)
                {
                    Database.EnqueueEdit(SaveLocal);
                    return;
                }

                LastSaveAttempt = GetTimeMillis();

                if (NakamaController.Session != null)// && NakamaController.Session.Created)
                {
                    //Debug.Log(new RText("SAVE LOCAL PLAYER - NakUserId = " + NakamaController.Session.UserId));//, Color.magenta));
                    nakamaUserId = NakamaController.Session.UserId;
                }

                deviceId = NakamaController.GetDeviceId();
                saveTime = GetTimeMillis();

                //Debug.Log("Save Player: " + deviceId + " :: " + saveTime);
                base.SaveLocal();
            }

            public void Verify()
            {
                //Player localPlayer = Load();
            }
        }

        [Serializable]
        public class Stats : PersistentDataRemote<Stats>
        {
            public int total_flowers_buffed_pets;
            public int level_flowers_buffed_picked;
            public void RecordPetBuffApplied()
            {
                total_flowers_buffed_pets++;
                EnqueueSaveRemote();
            }
            public int GetTotalFlowersBuffedPets()
            {
                return total_flowers_buffed_pets;
            }
            public void RecordPetBuffedPlantPicked()
            {
                level_flowers_buffed_picked++;
            }
            public void ResetPetBuffedPlantPicked()
            {
                level_flowers_buffed_picked = 0;
            }
            public int GetPetBuffedPlantPicked()
            {
                return level_flowers_buffed_picked;
            }
        }

        [Serializable]
        public class Cooldowns : PersistentDataRemote<Cooldowns>
        {
            public long boosterFlowerPartyLastUsedTime;
            public long boosterFlowerSpinnerLastUsedTime;
            public long boosterInstantGrowLastUsedTime;
            public long boosterInstantIdleEarningsLastUsedTime;
        }

        [Serializable]
        public class StorageData : PersistentDataRemote<StorageData>
        {
            public List<int> storageLevels = new List<int>();
        }

        [Serializable]
        public class RewardedAdPlayTimes : PersistentDataRemote<RewardedAdPlayTimes>
        {
            public List<long> idleEarningsMultiplierAdTimes = new List<long>();
            public List<long> wheelOfFlowersAdTimes = new List<long>();
            public List<long> instaGrowAdTimes = new List<long>();
        }

        [Serializable]
        public class Tutorial : PersistentDataRemote<Tutorial>
        {
            public bool hasShownDedication;
            public bool freeFlowerPartyUsed;
            public int tutorialIndex = -1;
            public List<int> completedTutorialFlows = new List<int>();
        }

        [Serializable]
        public class SpinnerData : PersistentDataRemote<SpinnerData>
        {
            public bool hasSpunPreviously;
        }

        [Serializable]
        public class TutorialData : PersistentDataRemote<TutorialData>
        {
            public string tutorialStep;
            public bool completed;

            
            public static TutorialData Create(string key, bool status)
            {
                TutorialData tutorialData = new TutorialData();
                tutorialData.instanceId = "Tutorial_" + key;
                tutorialData.tutorialStep = key;
                tutorialData.completed = status;
                return tutorialData;
            }
        }

        [Serializable]
        public class PurchasedInventoryItem : PersistentDataRemote<PurchasedInventoryItem>
        {
            public string itemName;
            public int count;
            public int itemId;

            public static PurchasedInventoryItem Create(ToolType toolType)
            {
                PurchasedInventoryItem purchasedInventory = new PurchasedInventoryItem();
                purchasedInventory.instanceId = "PurchasedInventoryItem_" + toolType.ToString();
                purchasedInventory.itemName = toolType.ToString();
                purchasedInventory.count = 0;
                purchasedInventory.itemId = (int)toolType;
                return purchasedInventory;
            }

            public void UpdateCount(int increasedAmount)
            {
                count += increasedAmount;
            }
        }
    }
