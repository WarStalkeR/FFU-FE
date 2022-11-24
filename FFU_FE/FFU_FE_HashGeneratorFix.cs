#pragma warning disable CS0618
#pragma warning disable IDE1006
#pragma warning disable IDE0019
#pragma warning disable IDE0002

/*
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using MonoMod;
using UnityEngine.Networking;

namespace Assets.Scripts.Objects.Electrical {
	class patch_LogicHashGen : LogicHashGen {
		private long _savedId;
		private SimpleFabricatorBase _currentFabricator;
		public SimpleFabricatorBase CurrentFabricator {
			get {
				return _currentFabricator;
			}
			set {
				if (!(_currentFabricator == value)) {
					_currentFabricator = value;
					LogicChanged();
				}
			}
		}
		public override ThingSaveData SerializeSave() {
			LogicReaderSaveData logicReaderSaveData = new LogicReaderSaveData();
			ThingSaveData savedData = logicReaderSaveData;
			InitialiseSaveData(ref savedData);
			return savedData;
		}
		public override void DeserializeSave(ThingSaveData savedData) {
			base.DeserializeSave(savedData);
			LogicReaderSaveData logicReaderSaveData = savedData as LogicReaderSaveData;
			if (logicReaderSaveData != null) {
				_savedId = logicReaderSaveData.CurrentDeviceId;
			}
		}
		protected override void InitialiseSaveData(ref ThingSaveData savedData) {
			base.InitialiseSaveData(ref savedData);
			LogicReaderSaveData logicReaderSaveData = savedData as LogicReaderSaveData;
			if (logicReaderSaveData != null && CurrentFabricator) {
				logicReaderSaveData.CurrentDeviceId = CurrentFabricator.ReferenceId;
				logicReaderSaveData.InputIndex = 0;
			}
		}
		public override void OnFinishedLoad() {
			if (GameManager.IsServer) {
				CurrentFabricator = XmlSaveLoad.GetReferenceable(_savedId) as SimpleFabricatorBase;
			}
		}
		public override string GetContextualName(Interactable interactable) {
			if (interactable.Action == InteractableType.Button1) {
				return CurrentFabricator ? CurrentFabricator.DisplayName : InterfaceStrings.LogicNoDevice;
			}
			return base.GetContextualName(interactable);
		}
		public override void OnStreamToClient(NetworkConnection conn, ref List<StreamMessage> streamBuffer) {
			base.OnStreamToClient(conn, ref streamBuffer);
			if (!(CurrentFabricator == null)) {
				StreamMessage item = new StreamMessage {
					MsgType = 1077,
					Message = new SetHashGenMessage {
						FabricatorId = CurrentFabricator.netId,
						LogicReaderId = base.netId
					}
				};
				streamBuffer.Add(item);
			}
		}
		public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true) {
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance {
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			InteractableType action = interactable.Action;
			if (interaction.SourceSlot.Occupant is Screwdriver) {
				if (action == InteractableType.Button1) {
					SimpleFabricatorBase nextReadable = GetNextReadable(CurrentFabricator, base.InputNetwork1DevicesSorted, interaction.AltKey);
					if (!nextReadable) return delayedActionInstance.Fail(InterfaceStrings.LogicNoReadableDevices);
					delayedActionInstance.StateMessage = string.Format(InterfaceStrings.ChangeSettingTo, nextReadable.ToTooltip());
					if (!KeyManager.GetButton(KeyMap.QuantityModifier)) delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
					if (!doAction) return delayedActionInstance.Succeed();
					if (GameManager.IsServer) {
						NetworkServer.SendByChannelToAll(1077, new SetHashGenMessage {
							FabricatorId = nextReadable.netId,
							LogicReaderId = base.netId
						}, 0);
						CurrentFabricator = nextReadable;
						Setting = 0.0;
					}
					return delayedActionInstance.Succeed();
				}
			} else {
				if (action == InteractableType.Button1) {
					if (!doAction) return delayedActionInstance.Succeed();
					foreach (int key in InputPrefabs.PrefabReferences.Keys) InputPrefabs.PrefabReferences[key].SetVisible(false);
					if (InventoryManager.ParentHuman.IsLocalPlayer) {
						if (CurrentFabricator != null && CurrentFabricator) {
							if (InputPrefabs.ShowInputPanel("Recipe Hash Selector", null, CurrentFabricator.DynamicThings, CurrentFabricator.CurrentBuildState.ManufactureDat.MachinesTier)) {
								InputPrefabs.OnSubmit += InputFinished;
								StartCoroutine(WaitForDropdown());
							}
						} else {
							if (InputPrefabs.ShowInputPanel("Recipe Hash Selector", null, GetDynamicThings(), MachineTier.Max)) {
								InputPrefabs.OnSubmit += InputFinished;
								StartCoroutine(WaitForDropdown());
							}
						}
					}
					return delayedActionInstance.Succeed();
				}
			}
			return base.InteractWith(interactable, interaction, doAction);
		}
		public Dictionary<MachineTier, List<DynamicThing>> GetDynamicThings() {
			Dictionary<MachineTier, List<DynamicThing>> list = new Dictionary<MachineTier, List<DynamicThing>> { { MachineTier.Max, new List<DynamicThing>() } };
			foreach (int key in InputPrefabs.PrefabReferences.Keys) {
				if (InputPrefabs.PrefabReferences[key].Prefab != null && InputPrefabs.PrefabReferences[key].Prefab) list[MachineTier.Max].Add(InputPrefabs.PrefabReferences[key].Prefab);
			}
			return list;
		}
		[MonoModIgnore] private IEnumerator WaitForDropdown() { yield break; }
	}
}

namespace Assets.Scripts.Networking {
	public class SetHashGenMessage : ProcessedMessage {
		public NetworkInstanceId LogicReaderId;
		public NetworkInstanceId FabricatorId;
		public override void Process() {
			if (!GameManager.IsServer) {
				patch_LogicHashGen logicReader = ProcessedMessage.Find<patch_LogicHashGen>(LogicReaderId);
				SimpleFabricatorBase fabricator = ProcessedMessage.Find<SimpleFabricatorBase>(FabricatorId);
				if (logicReader == null || fabricator == null) {
					List<NetworkInstanceId> list = new List<NetworkInstanceId> { LogicReaderId, FabricatorId };
					Singleton<GameManager>.Instance.NetworkManager.StartCoroutine(base.WaitUntilFound(Process, Process, list, 10f, "LogicHashGen"));
				} else {
					logicReader.CurrentFabricator = fabricator;
				}
			}
		}
		public override void Serialize(NetworkWriter writer) {
			writer.Write(LogicReaderId);
			writer.Write(FabricatorId);
		}
		public override void Deserialize(NetworkReader reader) {
			LogicReaderId = reader.ReadNetworkId();
			FabricatorId = reader.ReadNetworkId();
		}
	}
}
*/