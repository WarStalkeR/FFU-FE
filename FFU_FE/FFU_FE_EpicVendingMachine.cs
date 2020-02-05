#pragma warning disable CS0626
#pragma warning disable CS0618
#pragma warning disable IDE1006
#pragma warning disable IDE0019

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using MonoMod;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Objects.Electrical {
	[MonoModReplace]
	public class VendingMachine : DeviceImportExport, ITradeable {
		public bool HasSomething {
			get {
				return _filledSlots > 0;
			}
		}

		public void SyncCurrentIndex(int value) {
			BaseAnimator.SetBool(HasContentsState, false);
			NetworkCurrentIndex = value;
			if (!CurrentSlot.IsInteractable && CurrentSlot.Occupant) StartCoroutine(WaitThenCheck());
		}

		public override ThingSaveData SerializeSave() {
			VendingMachineSaveData vendingMachineSaveData = new VendingMachineSaveData();
			ThingSaveData result = vendingMachineSaveData;
			InitialiseSaveData(ref result);
			return result;
		}

		public override void DeserializeSave(ThingSaveData savedData) {
			base.DeserializeSave(savedData);
			VendingMachineSaveData vendingMachineSaveData = savedData as VendingMachineSaveData;
			if (vendingMachineSaveData != null) NetworkCurrentIndex = vendingMachineSaveData.CurrentIndex;
		}

		protected override void InitialiseSaveData(ref ThingSaveData savedData) {
			base.InitialiseSaveData(ref savedData);
			VendingMachineSaveData vendingMachineSaveData = savedData as VendingMachineSaveData;
			if (vendingMachineSaveData != null) vendingMachineSaveData.CurrentIndex = CurrentIndex;
		}

		private int SetRequestFromHash(int hash) {
			int num = Slots.FindIndex(2, (Slot slot) => slot.Occupant && slot.Occupant.PrefabHash == hash);
			int result;
			if (num < 0) result = -1;
			else {
				RequestedHash = hash;
				result = num;
			}
			return result;
		}

		private IEnumerator WaitSetRequestFromHash(int hash) {
			int index = SetRequestFromHash(hash);
			if (index >= 2) {
				while (!Powered || IsLocked) {
					yield return _waitForFrame;
				}
				OnServer.Interact(InteractLock, 1, false);
				NetworkCurrentIndex = index;
				yield return _waitForDelay;
				OnServer.MoveToSlot(CurrentSlot.Occupant, ExportSlot);
				OnServer.Interact(InteractExport, 1, false);
				RequestedHash = 0;
				OnServer.Interact(InteractLock, 0, false);
			}
			yield break;
		}

		public override bool CanLogicWrite(LogicType logicType) {
			return logicType == LogicType.RequestHash || base.CanLogicWrite(logicType);
		}

		public override void SetLogicValue(LogicType logicType, double value) {
			base.SetLogicValue(logicType, value);
			if (logicType == LogicType.RequestHash) {
				bool isThread = GameManager.IsThread;
				if (isThread) {
					UnityMainThreadDispatcher.Instance().Enqueue(WaitSetRequestFromHash((int)value));
				} else {
					StartCoroutine(WaitSetRequestFromHash((int)value));
				}
			}
		}

		public override bool CanLogicRead(LogicType logicType) {
			return logicType == LogicType.Ratio || logicType == LogicType.Quantity || logicType == LogicType.RequestHash || base.CanLogicRead(logicType);
		}

		private void CalculateFilledSlots() {
			int num = 0;
			for (int i = 2; i < Slots.Count; i++) {
				Slot slot = Slots[i];
				if (!!slot.Occupant) num++;
			}
			_filledSlots = num;
		}

		public override double GetLogicValue(LogicType logicType) {
			double result;
			if (logicType != LogicType.Ratio) {
				if (logicType != LogicType.Quantity) {
					if (logicType != LogicType.RequestHash) {
						result = base.GetLogicValue(logicType);
					} else {
						result = RequestedHash;
					}
				} else {
					result = _filledSlots;
				}
			} else {
				result = _filledSlots / 100f;
			}
			return result;
		}

		private IEnumerator WaitThenCheck() {
			yield return Yielders.WaitForSeconds(0.2f);
			BaseAnimator.SetBool(HasContentsState, CurrentSlot.Occupant);
			yield break;
		}

		public void PreviousContentsClear() {
			Screen.material.mainTexture = null;
			Screen.material.SetTexture("_EmissionMap", null);
		}

		public void PreviousContentsShow() {
			Texture2D texture2D = CurrentSlot.Occupant ? CurrentSlot.Occupant.GetThumbnail().texture : null;
			Screen.material.mainTexture = texture2D;
			Screen.material.SetTexture("_EmissionMap", texture2D);
		}

		public Slot CurrentSlot {
			get {
				return Slots[CurrentIndex];
			}
		}

		public override void OnServerTick() {
			base.OnServerTick();
			if (!(!OnOff || !Powered)) {
				if (ImportingThing == null) TryChuteImport();
			}
		}

		public override void OnChildEnterInventory(DynamicThing newChild) {
			base.OnChildEnterInventory(newChild);
			if (newChild.ParentSlot == CurrentSlot && !CurrentSlot.IsInteractable) {
				BaseAnimator.SetBool(HasContentsState, false);
				StartCoroutine(WaitThenCheck());
			}
			CalculateFilledSlots();
		}

		public override void OnChildExitInventory(DynamicThing previousChild) {
			if (previousChild.ParentSlot == CurrentSlot && !CurrentSlot.IsInteractable) BaseAnimator.SetBool(HasContentsState, false);
			base.OnChildExitInventory(previousChild);
			if (!BeingDestroyed) {
				BaseAnimator.SetBool(HasContentsState, false);
				StartCoroutine(WaitThenCheck());
				CalculateFilledSlots();
			}
		}

		public Slot SamplePlanBackward() {
			int num = CurrentIndex;
			bool vBackward = true;
			int num2 = 0;
			while (vBackward) {
				num--;
				if (num < 0) num = Slots.Count - 1;
				Slot slot = Slots[num];
				if (!slot.IsInteractable && slot.Occupant) vBackward = false;
				if (num2 > Slots.Count) {
					num = CurrentIndex;
					vBackward = false;
				}
				num2++;
			}
			return Slots[num];
		}

		public Slot SamplePlanForward() {
			int num = CurrentIndex;
			bool vForward = true;
			int num2 = 0;
			while (vForward) {
				num++;
				if (num >= Slots.Count) num = 0;
				Slot slot = Slots[num];
				if (!slot.IsInteractable && slot.Occupant) vForward = false;
				if (num2 > Slots.Count) {
					num = CurrentIndex;
					vForward = false;
				}
				num2++;
			}
			return Slots[num];
		}

		public void PlanForward() {
			NetworkCurrentIndex = SamplePlanForward().SlotId;
		}

		public void PlanBackward() {
			NetworkCurrentIndex = SamplePlanBackward().SlotId;
		}

		public override void OnCustomImportFinished() {
			base.OnCustomImportFinished();
			if (!!GameManager.IsServer) {
				if (ImportingThing) {
					foreach (Slot slot in Slots) {
						bool isInteractable = slot.IsInteractable;
						if (!isInteractable) {
							if (!slot.Occupant) {
								OnServer.MoveToSlot(ImportingThing, slot);
								break;
							}
						}
					}
				}
				OnServer.Interact(InteractImport, 0, false);
				if (!(GameManager.GameState != GameState.Running)) {
					if (!CurrentSlot.Occupant) PlanForward();
				}
			}
		}

		public override string GetContextualName(Interactable interactable) {
			string result;
			string vendItemColor = "#DDDDDD";
			string vendItemQuantity = "";
			string quantityText = CurrentSlot.Occupant != null ? CurrentSlot.Occupant ? CurrentSlot.Occupant.GetQuantityText() : "" : "";
			if (!string.IsNullOrEmpty(quantityText)) quantityText = quantityText
				.Replace("<size","")
				.Replace("</size>", "")
				.Replace("0%>", "")
				.Replace("=0", "")
				.Replace("=1", "")
				.Replace("=2", "")
				.Replace("=3", "")
				.Replace("=4", "")
				.Replace("=5", "")
				.Replace("=6", "")
				.Replace("=7", "")
				.Replace("=8", "")
				.Replace("=9", "")
				.Replace("\n", "");
			if (CurrentSlot.Occupant != null)
				if (CurrentSlot.Occupant)
					if (!string.IsNullOrEmpty(quantityText))
						if (Regex.Match(quantityText, @"\d+").Success || quantityText.ToLower().Contains("charge"))
							vendItemQuantity = " (" + quantityText.Replace("Charge", "").Replace("charge", "") + ")";
			if (interactable.Action == InteractableType.Activate) result = CurrentSlot.Occupant ? "<color=" + vendItemColor + ">" + ActionStrings.Vend + " " + CurrentSlot.Occupant.DisplayName + vendItemQuantity + "</color>" : ActionStrings.Vend;
			else {
				if ((interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2) && KeyManager.GetButton(KeyMap.QuantityModifier)) result = "<color=" + vendItemColor + ">" + "Access Inventory List" + "</color>";
				else if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2) result = CurrentSlot.Occupant ? "<color=" + vendItemColor + ">" + CurrentSlot.Occupant.DisplayName + vendItemQuantity + "\n" + "<size=75%>" + Localization.ParseTooltip("Hold {KEY:QuantityModifier} while interacting to access Inventory List.", false) + "</size>" + "</color>" : CurrentSlot.DisplayName;
				else result = base.GetContextualName(interactable);
			}
			return result;
		}

		public override void OnCustomExportReady() {
			base.OnCustomExportReady();
			PlanForward();
		}

		public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true) {
			if (interactable.Action == InteractableType.Activate || interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2) {
				DelayedActionInstance delayedActionInstance = new DelayedActionInstance {
					Duration = 0f,
					ActionMessage = interactable.ContextualName
				};
				bool isLocked = IsLocked;
				if (isLocked) return delayedActionInstance.Fail(HelpTextDevice.DeviceLocked);
				if (!IsAuthorized(interaction.SourceThing)) return delayedActionInstance.Fail(Localization.ParseTooltip("Unable to interact as you do not have the required {SLOT:AccessCard}", false));
				if (!OnOff) return delayedActionInstance.Fail(HelpTextDevice.DeviceNotOn);
				if (!Powered) return delayedActionInstance.Fail(HelpTextDevice.DeviceNoPower);
				if (Exporting != 0) return delayedActionInstance.Fail(HelpTextDevice.DeviceLocked);
				if (interactable.Action == InteractableType.Activate) {
					if (!CurrentSlot.Occupant || !HasSomething) return delayedActionInstance.Fail("Nothing selected to dispense");
					if (!doAction) return delayedActionInstance.Succeed();
					bool isServer = GameManager.IsServer;
					if (isServer) {
						OnServer.MoveToSlot(CurrentSlot.Occupant, ExportSlot);
						OnServer.Interact(InteractExport, 1, false);
					}
					return delayedActionInstance.Succeed();
				} else {
					if ((interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2) && KeyManager.GetButton(KeyMap.QuantityModifier)) {
						if (!HasSomething) {
							delayedActionInstance.ActionMessage = "Access Inventory List";
							return delayedActionInstance.Fail("Nothing inside to list");
						}
						if (!doAction) return delayedActionInstance.Succeed();
						foreach (int key in InputPrefabs.PrefabReferences.Keys) InputPrefabs.PrefabReferences[key].SetVisible(false);
						if (InputPrefabs.ShowInputPanel("Vending Machine", null, GetDynamicThings(), MachineTier.Max)) {
							InputPrefabs.OnSubmit += InputFinished;
						}
						return delayedActionInstance.Succeed();
					}
					else if (interactable.Action == InteractableType.Button1) {
						if (!HasSomething) {
							delayedActionInstance.ActionMessage = ActionStrings.Down;
							return delayedActionInstance.Fail("Nothing inside to change too");
						}
						Slot slot = SamplePlanBackward();
						delayedActionInstance.StateMessage = string.Format(InterfaceStrings.ChangeSettingTo, slot.Occupant ? slot.Occupant.ToTooltip() : CurrentSlot.ToTooltip());
						if (!doAction) return delayedActionInstance.Succeed();
						bool isServer2 = GameManager.IsServer;
						if (isServer2) PlanBackward();
						return delayedActionInstance.Succeed();
					}
					else if (interactable.Action == InteractableType.Button2) {
						if (!HasSomething) {
							delayedActionInstance.ActionMessage = ActionStrings.Up;
							return delayedActionInstance.Fail("Nothing inside to change too");
						}
						Slot slot2 = SamplePlanForward();
						delayedActionInstance.StateMessage = string.Format(InterfaceStrings.ChangeSettingTo, slot2.Occupant ? slot2.Occupant.ToTooltip() : CurrentSlot.ToTooltip());
						if (!doAction) return delayedActionInstance.Succeed();
						bool isServer3 = GameManager.IsServer;
						if (isServer3) PlanForward();
						return delayedActionInstance.Succeed();
					}
				}
			}
			return base.InteractWith(interactable, interaction, doAction);
		}

		public void InputFinished(DynamicThing prefab) {
			if (prefab != null) {
				foreach (Slot slot in Slots) {
					if (slot.Occupant != null) {
						if (slot.Occupant.PrefabHash == prefab.PrefabHash) {
							CurrentIndex = Slots.IndexOf(slot);
							NetworkCurrentIndex = Slots.IndexOf(slot);
							break;
						}
					}
				}
			}
		}

		public List<DynamicThing> GetContents() {
			List<DynamicThing> list = new List<DynamicThing>();
			foreach (Slot slot in Slots) {
				if (slot.Occupant) list.Add(slot.Occupant);
			}
			return list;
		}

		public Dictionary<MachineTier, List<DynamicThing>> GetDynamicThings() {
			Dictionary<MachineTier, List<DynamicThing>> list = new Dictionary<MachineTier, List<DynamicThing>> {{ MachineTier.Max, new List<DynamicThing>() }};
			foreach (Slot slot in Slots) {
				if (slot.Occupant) list[MachineTier.Max].Add(slot.Occupant);
			}
			return list;
		}

		public Dictionary<int, Slot> GetOccupiedSlots() {
			Dictionary<int, Slot> dictionary = new Dictionary<int, Slot>();
			for (int i = 2; i < Slots.Count; i++) {
				if (Slots[i].Occupant) dictionary.Add(i, Slots[i]);
			}
			return dictionary;
		}

		private void UNetVersion() {
		}

		public int NetworkCurrentIndex {
			get {
				return CurrentIndex;
			}
			set {
				uint dirtyBit = 1u;
				if (NetworkServer.localClientActive && !syncVarHookGuard) {
					syncVarHookGuard = true;
					SyncCurrentIndex(value);
					syncVarHookGuard = false;
				}
				SetSyncVar(value, ref CurrentIndex, dirtyBit);
			}
		}

		public override bool OnSerialize(NetworkWriter writer, bool forceAll) {
			bool vSerialize = base.OnSerialize(writer, forceAll);
			if (forceAll) {
				writer.WritePackedUInt32((uint)CurrentIndex);
				return true;
			}
			bool vSyncVar = false;
			if ((syncVarDirtyBits & 1u) != 0u) {
				if (!vSyncVar) {
					writer.WritePackedUInt32(syncVarDirtyBits);
					vSyncVar = true;
				}
				writer.WritePackedUInt32((uint)CurrentIndex);
			}
			if (!vSyncVar) writer.WritePackedUInt32(syncVarDirtyBits);
			return vSyncVar || vSerialize;
		}

		public override void OnDeserialize(NetworkReader reader, bool initialState) {
			base.OnDeserialize(reader, initialState);
			if (initialState) {
				CurrentIndex = (int)reader.ReadPackedUInt32();
				return;
			}
			int num = (int)reader.ReadPackedUInt32();
			if ((num & 1) != 0) {
				SyncCurrentIndex((int)reader.ReadPackedUInt32());
			}
		}

		public override void PreStartClient() {
			base.PreStartClient();
		}

		public const int StartIndex = 2;
		public const int StorageSlots = 100;
		public Transform PreviewTransform;
		[SyncVar(hook = "SyncCurrentIndex")] public int CurrentIndex = 2;
		private readonly WaitForSeconds _waitForDelay = new WaitForSeconds(0.5f);
		private readonly WaitForEndOfFrame _waitForFrame = new WaitForEndOfFrame();
		public int RequestedHash;
		private int _filledSlots;
		public static int HasContentsState = Animator.StringToHash("HasContents");
		public Renderer Screen;
	}
}