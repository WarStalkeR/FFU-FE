#pragma warning disable CS0626
#pragma warning disable CS0649
#pragma warning disable IDE0002
#pragma warning disable IDE1006
#pragma warning disable IDE0019

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Assets.Scripts.UI;
using UnityEngine;
using MonoMod;

namespace Assets.Scripts.Objects.Electrical {
	[MonoModReplace] public class VendingMachine : DeviceImportExport, ITradeable {
	/// Sadly, full replace is a must, otherwise just pointing on power switch with crash the game.
		public const int StartIndex = 2;
		public const int StorageSlots = 100;
		public Transform PreviewTransform;
		private int _currentIndex = 2;
		private readonly WaitForSeconds _waitForDelay = new WaitForSeconds(0.5f);
		private readonly WaitForEndOfFrame _waitForFrame = new WaitForEndOfFrame();
		public int RequestedHash;
		private int _filledSlots;
		public static readonly int HasContentsState = Animator.StringToHash("HasContents");
		private static readonly int VendButtonHash = Animator.StringToHash("VendButton");
		private static readonly int SelectButtonHash = Animator.StringToHash("SelectButton");
		private static readonly int VendButtonEnabledHash = Animator.StringToHash("VendButtonEnabled");
		private static readonly int VendButtonDisabledHash = Animator.StringToHash("VendButtonDisabled");
		public Renderer Screen;
		private GameAudioSource _vendButtonAudio;
		[ByteArraySync] public int CurrentIndex {
			get { 
				return _currentIndex;
			}
			set {
				BaseAnimator.SetBool(HasContentsState, value: false);
				_currentIndex = value;
				if (NetworkManager.IsServer) {
					base.NetworkUpdateFlags |= 256;
				}
				if (!CurrentSlot.IsInteractable && (bool)CurrentSlot.Occupant) {
					StartCoroutine(WaitThenCheck());
				}
			}
		}
		public bool HasSomething => _filledSlots > 0;
		public Slot CurrentSlot => Slots[CurrentIndex];
		public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType) {
			base.BuildUpdate(writer, networkUpdateType);
			if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType)) writer.WriteInt32(CurrentIndex);
		}
		public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType) {
			base.ProcessUpdate(reader, networkUpdateType);
			if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType)) CurrentIndex = reader.ReadInt32();
		}
		public override void SerializeOnJoin(RocketBinaryWriter writer) {
			base.SerializeOnJoin(writer);
			writer.WriteInt32(CurrentIndex);
		}
		public override void DeserializeOnJoin(RocketBinaryReader reader) {
			base.DeserializeOnJoin(reader);
			CurrentIndex = reader.ReadInt32();
		}
		public override ThingSaveData SerializeSave() {
			ThingSaveData savedData = new VendingMachineSaveData();
			InitialiseSaveData(ref savedData);
			return savedData;
		}
		public override void DeserializeSave(ThingSaveData savedData) {
			base.DeserializeSave(savedData);
			VendingMachineSaveData vendingMachineSaveData = savedData as VendingMachineSaveData;
			if (vendingMachineSaveData != null) CurrentIndex = vendingMachineSaveData.CurrentIndex;
		}
		protected override void InitialiseSaveData(ref ThingSaveData savedData) {
			base.InitialiseSaveData(ref savedData);
			VendingMachineSaveData vendingMachineSaveData = savedData as VendingMachineSaveData;
			if (vendingMachineSaveData != null) vendingMachineSaveData.CurrentIndex = CurrentIndex;
		}
		private int SetRequestFromHash(int hash) {
			int num = Slots.FindIndex(2, (Slot slot) => (bool)slot.Occupant && slot.Occupant.PrefabHash == hash);
			if (num < 0) return -1;
			RequestedHash = hash;
			return num;
		}
		private IEnumerator WaitSetRequestFromHash(int hash) {
			int index = SetRequestFromHash(hash);
			if (index >= 2) {
				while (!Powered || IsLocked) yield return _waitForFrame;
				OnServer.Interact(base.InteractLock, 1);
				CurrentIndex = index;
				yield return _waitForDelay;
				OnServer.MoveToSlot(CurrentSlot.Occupant, ExportSlot);
				OnServer.Interact(base.InteractExport, 1);
				RequestedHash = 0;
				OnServer.Interact(base.InteractLock, 0);
			}
		}
		public override bool CanLogicWrite(LogicType logicType) {
			if (logicType == LogicType.RequestHash) return true;
			return base.CanLogicWrite(logicType);
		}
		public override void SetLogicValue(LogicType logicType, double value) {
			base.SetLogicValue(logicType, value);
			if (logicType == LogicType.RequestHash) {
				if (GameManager.IsThread) UnityMainThreadDispatcher.Instance().Enqueue(WaitSetRequestFromHash((int)value));
				else StartCoroutine(WaitSetRequestFromHash((int)value));
			}
		}
		public override bool CanLogicRead(LogicType logicType) {
			switch (logicType) {
				case LogicType.RequestHash:
				return true;
				case LogicType.Quantity:
				return true;
				case LogicType.Ratio:
				return true;
				default:
				return base.CanLogicRead(logicType);
			}
		}
		private void CalculateFilledSlots() {
			int num = 0;
			for (int i = 2; i < Slots.Count; i++) 
				if ((bool)Slots[i].Occupant) num++;
			_filledSlots = num;
		}
		public override double GetLogicValue(LogicType logicType) {
			switch (logicType) {
				case LogicType.RequestHash:
				return RequestedHash;
				case LogicType.Quantity:
				return _filledSlots;
				case LogicType.Ratio:
				return (float)_filledSlots / 100f;
				default:
				return base.GetLogicValue(logicType);
			}
		}
		private IEnumerator WaitThenCheck() {
			yield return Yielders.WaitForSeconds(0.2f);
			BaseAnimator.SetBool(HasContentsState, CurrentSlot.Occupant);
		}
		public void PreviousContentsClear() {
			Screen.material.mainTexture = null;
			Screen.material.SetTexture("_EmissionMap", null);
		}
		public void PreviousContentsShow() {
			Texture2D texture2D = (CurrentSlot.Occupant ? CurrentSlot.Occupant.GetThumbnail().texture : null);
			Screen.material.mainTexture = texture2D;
			Screen.material.SetTexture("_EmissionMap", texture2D);
		}
		protected override void OnServerImportTick() {
			if (IsNextImportReady) TryChuteImport();
		}
		public override void OnChildEnterInventory(DynamicThing newChild) {
			base.OnChildEnterInventory(newChild);
			if (newChild.ParentSlot == CurrentSlot && !CurrentSlot.IsInteractable) {
				BaseAnimator.SetBool(HasContentsState, value: false);
				StartCoroutine(WaitThenCheck());
			}
			CalculateFilledSlots();
		}
		public override void OnChildExitInventory(DynamicThing previousChild) {
			if (previousChild.ParentSlot == CurrentSlot && !CurrentSlot.IsInteractable) BaseAnimator.SetBool(HasContentsState, value: false);
			base.OnChildExitInventory(previousChild);
			if (!base.BeingDestroyed) {
				BaseAnimator.SetBool(HasContentsState, value: false);
				StartCoroutine(WaitThenCheck());
				CalculateFilledSlots();
				TryProcessImport();
			}
		}
		public Slot SamplePlanBackward() {
			int num = CurrentIndex;
			bool flag = true;
			int num2 = 0;
			while (flag) {
				num--;
				if (num < 0) num = Slots.Count - 1;
				Slot slot = Slots[num];
				if (!slot.IsInteractable && (bool)slot.Occupant) flag = false;
				if (num2 > Slots.Count) {
					num = CurrentIndex;
					flag = false;
				}
				num2++;
			}
			return Slots[num];
		}
		public Slot SamplePlanForward() {
			int num = CurrentIndex;
			bool flag = true;
			int num2 = 0;
			while (flag) {
				num++;
				if (num >= Slots.Count) num = 0;
				Slot slot = Slots[num];
				if (!slot.IsInteractable && (bool)slot.Occupant) flag = false;
				if (num2 > Slots.Count) {
					num = CurrentIndex;
					flag = false;
				}
				num2++;
			}
			return Slots[num];
		}
		public void PlanForward() {
			CurrentIndex = SamplePlanForward().SlotId;
		}
		public void PlanBackward() {
			CurrentIndex = SamplePlanBackward().SlotId;
		}
		public override void OnImportClosingComplete() {
			base.OnImportClosingComplete();
			if (GameManager.RunSimulation) {
				TryProcessImport();
				if (GameManager.GameState == GameState.Running && !CurrentSlot.Occupant) {
					PlanForward();
				}
			}
		}
		protected virtual void TryProcessImport() {
			if (ImportingThing != null) {
				for (int i = 0; i < Slots.Count; i++) {
					Slot slot = Slots[i];
					if (!slot.IsInteractable) {
						if (!(slot.Occupant != null)) {
							OnServer.MoveToSlot(ImportingThing, slot);
							break;
						}
						if (i == Slots.Count - 1) {
							PlayNetworkSound(FabricatorBase.ImportErrorHash, base.InteractOnOff.Collider.transform.localPosition);
						}
					}
				}
			}
			OnServer.Interact(base.InteractImport, 0);
		}
		public override string GetContextualName(Interactable interactable) {
		/// Show "Access Inventory List" notification.
			string vendItemColor = "#DDDDDD";
			if (interactable.Action == InteractableType.Activate) {
				if (!CurrentSlot.Occupant) return ActionStrings.Vend;
				return ActionStrings.Vend + " " + CurrentSlot.Occupant.DisplayName + " " + CurrentSlot.Occupant.GetQuantityText();
			}
			if ((interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2) && (KeyManager.GetButton(KeyCode.LeftShift) || KeyManager.GetButton(KeyCode.RightShift))) {
				if (!CurrentSlot.Occupant) return CurrentSlot.DisplayName;
				GetOccupiedSlots();
				return "<color=" + vendItemColor + ">" + "Access Inventory List" + "</color>" + "\n" +
					"<color=" + vendItemColor + ">" + "<size=75%>" + Localization.ParseTooltip($"Machine storage capacity status: <color=orange>{_filledSlots}/{StorageSlots}</color>", false) + "</size>" + "</color>";
			} else if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2) {
				if (!CurrentSlot.Occupant) return CurrentSlot.DisplayName;
				return CurrentSlot.Occupant.DisplayName + " " + CurrentSlot.Occupant.GetQuantityText() + "\n" +
					"<color=" + vendItemColor + ">" + "<size=75%>" + Localization.ParseTooltip("Hold <color=orange>SHIFT</color> while interacting to access Inventory List.", false) + "</size>" + "</color>";
			}
			return base.GetContextualName(interactable);
		}
		public override void OnExportClosingComplete() {
			base.OnExportClosingComplete();
			PlanForward();
		}
		public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true) {
		/// Show "Access Inventory List" notification.
			if (interactable.Action == InteractableType.Activate || interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2) {
				DelayedActionInstance delayedActionInstance = new DelayedActionInstance {
					Duration = 0f,
					ActionMessage = interactable.ContextualName
				};
				if (IsLocked) return delayedActionInstance.Fail(HelpTextDevice.DeviceLocked);
				if (!IsAuthorized(interaction.SourceThing)) return delayedActionInstance.Fail(Localization.ParseTooltip("Unable to interact as you do not have the required {SLOT:AccessCard}"));
				switch (interactable.Action) {
					case InteractableType.Activate:
					if (!OnOff) return delayedActionInstance.Fail(HelpTextDevice.DeviceNotOn);
					if (!Powered) return delayedActionInstance.Fail(HelpTextDevice.DeviceNoPower);
					if (Exporting != 0) return delayedActionInstance.Fail(HelpTextDevice.DeviceLocked);
					if (!CurrentSlot.Occupant || !HasSomething) return delayedActionInstance.Fail("Nothing selected to dispense");
					if (!doAction) return delayedActionInstance.Succeed();
					PlaySound(VendButtonHash);
					if (GameManager.RunSimulation) {
						OnServer.MoveToSlot(CurrentSlot.Occupant, ExportSlot);
						OnServer.Interact(base.InteractExport, 1);
					}
					return delayedActionInstance.Succeed();
					case InteractableType.Button1: {
						if (!HasSomething) {
							delayedActionInstance.ActionMessage = ActionStrings.Down;
							return delayedActionInstance.Fail("Nothing inside to change to");
						}
						if (KeyManager.GetButton(KeyCode.LeftShift) || KeyManager.GetButton(KeyCode.RightShift)) {
							if (!doAction) return delayedActionInstance.Succeed();
							SortStoredItems();
							foreach (int key in InputPrefabs.PrefabReferences.Keys) InputPrefabs.PrefabReferences[key].SetVisible(false);
							if (InputPrefabs.ShowInputPanel("Vending Machine", null, GetDynamicThings(), MachineTier.Max)) InputPrefabs.OnSubmit += InputFinished;
							return delayedActionInstance.Succeed();
						}
						Slot slot2 = SamplePlanBackward();
						delayedActionInstance.StateMessage = string.Format(InterfaceStrings.ChangeSettingTo, slot2.Occupant ? slot2.Occupant.ToTooltip() : CurrentSlot.ToTooltip());
						if (!doAction) return delayedActionInstance.Succeed();
						PlaySound(SelectButtonHash);
						if (GameManager.RunSimulation) PlanBackward();
						return delayedActionInstance.Succeed();
					}
					case InteractableType.Button2: {
						if (!HasSomething) {
							delayedActionInstance.ActionMessage = ActionStrings.Up;
							return delayedActionInstance.Fail("Nothing inside to change to");
						}
						if (KeyManager.GetButton(KeyCode.LeftShift) || KeyManager.GetButton(KeyCode.RightShift)) {
							if (!doAction) return delayedActionInstance.Succeed();
							SortStoredItems();
							foreach (int key in InputPrefabs.PrefabReferences.Keys) InputPrefabs.PrefabReferences[key].SetVisible(false);
							if (InputPrefabs.ShowInputPanel("Vending Machine", null, GetDynamicThings(), MachineTier.Max)) InputPrefabs.OnSubmit += InputFinished;
							return delayedActionInstance.Succeed();
						}
						Slot slot = SamplePlanForward();
						delayedActionInstance.StateMessage = string.Format(InterfaceStrings.ChangeSettingTo, slot.Occupant ? slot.Occupant.ToTooltip() : CurrentSlot.ToTooltip());
						if (!doAction) return delayedActionInstance.Succeed();
						PlaySound(SelectButtonHash);
						if (GameManager.RunSimulation) PlanForward();
						return delayedActionInstance.Succeed();
					}
				}
			}
			return base.InteractWith(interactable, interaction, doAction);
		}
		private Dictionary<MachineTier, List<DynamicThing>> GetDynamicThings() {
		/// Create dynamic list for all stored items in Vending Machine.
			Dictionary<MachineTier, List<DynamicThing>> list = new Dictionary<MachineTier, List<DynamicThing>> { { MachineTier.Max, new List<DynamicThing>() } };
			foreach (Slot slot in Slots) {
				if (slot.Occupant) list[MachineTier.Max].Add(slot.Occupant);
			}
			return list;
		}
		private void InputFinished(DynamicThing prefab) {
		/// Return first slot in Vending Machine that has chosen item.
			if (prefab != null) {
				foreach (Slot slot in Slots) {
					if (slot.Occupant != null) {
						if (slot.Occupant.PrefabHash == prefab.PrefabHash) {
							CurrentIndex = Slots.IndexOf(slot);
							break;
						}
					}
				}
			}
		}
		private void SortStoredItems() {
		/// Sort all items stored in Vending Machine by PrefabHash.
			/*Slots = Slots.OrderBy(i => (bool)i.Occupant ? i.Occupant.PrefabHash : int.MaxValue).ToList();
			for (int i = 0; i < Slots.Count; i++) {
				Slot sItem = Slots[i];
				if (!sItem.IsInteractable) {
					if ((bool)sItem.Occupant) {
						OnServer.MoveToSlot(sItem.Occupant, sItem);
					}
				}
			}
			OnServer.Interact(base.InteractImport, 0);*/
		}
		public List<DynamicThing> GetContents() {
			List<DynamicThing> list = new List<DynamicThing>();
			foreach (Slot slot in Slots) 
				if ((bool)slot.Occupant) list.Add(slot.Occupant);
			return list;
		}
		public Dictionary<int, Slot> GetOccupiedSlots() {
			Dictionary<int, Slot> dictionary = new Dictionary<int, Slot>();
			for (int i = 2; i < Slots.Count; i++) 
				if ((bool)Slots[i].Occupant) dictionary.Add(i, Slots[i]);
			return dictionary;
		}
		public void PlayVendButtonEnabledSound() {
			if (_vendButtonAudio == null) _vendButtonAudio = GetAudioSource(GetAudioEvent(VendButtonHash).Channel);
			if (Powered && (!_vendButtonAudio.isPlaying || _vendButtonAudio.CurrentClips?.NameHash != VendButtonHash)) PlaySound(VendButtonEnabledHash);
		}
		public void PlayVendButtonDisabledSound() {
			if (_vendButtonAudio == null) _vendButtonAudio = GetAudioSource(GetAudioEvent(VendButtonHash).Channel);
			if (Powered && (!_vendButtonAudio.isPlaying || _vendButtonAudio.CurrentClips?.NameHash != VendButtonHash)) PlaySound(VendButtonDisabledHash);
		}
	}
}