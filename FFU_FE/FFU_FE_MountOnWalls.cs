#pragma warning disable CS0626
#pragma warning disable CS0649
#pragma warning disable IDE1006
#pragma warning disable IDE0019

using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using MonoMod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;

namespace Assets.Scripts.Inventory {
	public class patch_InventoryManager : InventoryManager {
		[MonoModIgnore] private PassiveTooltip tooltip;
		[MonoModIgnore] private Mothership _parentMothership;
		[MonoModIgnore] private Vector3 _cursorPosition;
		[MonoModIgnore] private Grid3 _localGrid;
		[MonoModIgnore] private Vector3 _worldGrid;
		[MonoModIgnore] private Vector3 _worldMasterGrid;
		[MonoModIgnore] private bool _usingAutoplace;
		[MonoModIgnore] private static Quaternion _storedMothershipRotation;
		[MonoModIgnore] private static readonly int RotateBlueprintHash;
		[MonoModIgnore] private int _lastHorizontalFace;
		[MonoModIgnore] private Vector3 _usePrimaryPosition;
		[MonoModIgnore] private Quaternion _usePrimaryRotation;
		[MonoModIgnore] private void UseItemComplete() { }
		[MonoModIgnore] private void UsePrimaryComplete() { }
		[MonoModIgnore] private bool CanAttackWith(Collider selectedCollider) { return true; }
		[MonoModIgnore] private IEnumerator WaitUntilDone(DelegateEvent onFinished, float timeToWait, Structure structure) { return null; }
		[MonoModIgnore] private IEnumerator WaitUntilDone(DelegateEvent onFinished, Thing.DelayedActionInstance attack, int actionSoundHash, int actionCompleteSoundHash) { return null; }
		public static readonly string enablePlacement = "Turn on <color=orange>SCROLL LOCK</color> to enable the <color=yellow>experimental</color> placement mode (to place almost anywhere).";
		public static readonly string disablePlacement = "<color=yellow><color=orange>Warning!</color> Experimental placement mode is enabled. Don't forget to turn off <color=orange>SCROLL LOCK</color> to disable it.</color>";
		public static readonly string realignPlacement = "Use {KEY:PrecisionPlace} to <color=green>re-align</color> current object to the mountable surface, if it isn't aligned properly.";
        [MonoModReplace] private void PlacementMode() {
		/// Experimental placement mode implementation (via SCROLL LOCK).
			tooltip = new PassiveTooltip(toDefault: true);
			bool isVisibleTool = (bool)(ActiveHand.Slot.Occupant as Constructor) || ConstructionPanel.IsVisible;
			if ((!isVisibleTool && !IsAuthoringMode) || KeyManager.GetMouseDown("Secondary")) {
				CancelPlacement();
				return;
			}
			MultiConstructor multiConstructor = ActiveHand.Slot.Occupant as MultiConstructor;
			if (!ConstructionCursor) return;
			DynamicThing trackedThing = CameraController.Instance.TrackedThing;
			Transform thingTransform = trackedThing.ThingTransform;
			Vector3 cameraForwardGrid = InputHelpers.GetCameraForwardGrid((ConstructionCursor.GridSize > 0.5f) ? 0.3f : 0.6f, ConstructionCursor.GetCursorOffset);
			_parentMothership = (CursorManager.CursorThing ? CursorManager.CursorThing.GridController.ParentMothership : Mothership.GetNearbyMothership(cameraForwardGrid));
			if ((bool)_parentMothership) {
				cameraForwardGrid += thingTransform.position - trackedThing.ActiveRigidbody.position;
			}
			ConstructionCursor.GridController = ((_parentMothership != null) ? _parentMothership.GridController : GridController.World);
			ConstructionCursor.Mothership = _parentMothership;
			ConstructionCursor.Position = ConstructionCursor.ThingTransformPosition;
			ConstructionCursor.Rotation = ConstructionCursor.ThingTransform.rotation;
			CursorManager.SetSelectionVisibility(ShowUi);
			CursorManager.Instance.CursorSelectionHighlighter.transform.rotation = Quaternion.identity;
			_cursorPosition = Vector3.zero;
			_localGrid = ConstructionCursor.GetLocalGrid(cameraForwardGrid);
			_worldGrid = ConstructionCursor.GetWorldGrid(cameraForwardGrid);
			_worldMasterGrid = ConstructionCursor.GridController.ClampWorld(cameraForwardGrid);
			bool notBlocked = !multiConstructor || multiConstructor.CanBuild(ConstructionPanel.BuildIndex);
			Structure.CanMountResult canMountResult = default(Structure.CanMountResult);
			bool modePlacementExperimental = Control.IsKeyLocked(Keys.Scroll);
			switch (ConstructionCursor.PlacementType) {
				case PlacementSnap.Grid:
				if (!KeyManager.GetButton(KeyMap.QuantityModifier)) {
					_usingAutoplace = false;
					_cursorPosition = _worldGrid;
					ConstructionCursor.ThingTransformPosition = _cursorPosition;
					Quaternion quaternion2 = ((ConstructionCursor.Mothership == null) ? Quaternion.identity : ConstructionCursor.Mothership.ThingTransform.rotation);
					if (quaternion2 != _storedMothershipRotation) {
						ConstructionCursor.ThingTransform.rotation = quaternion2 * Quaternion.Inverse(_storedMothershipRotation) * ConstructionCursor.ThingTransform.rotation;
						_storedMothershipRotation = quaternion2;
					}
					if ((ConstructionCursor.RotationAxis & RotationAxis.Y) != 0) {
						if (KeyManager.GetButtonUp(KeyMap.RotateLeft)) {
							ConstructionCursor.ThingTransform.Rotate(quaternion2 * Vector3.up, 90f, Space.World);
							UIAudioManager.Play(RotateBlueprintHash);
						}
						if (KeyManager.GetButtonUp(KeyMap.RotateRight)) {
							ConstructionCursor.ThingTransform.Rotate(quaternion2 * Vector3.up, -90f, Space.World);
							UIAudioManager.Play(RotateBlueprintHash);
						}
					}
					if ((ConstructionCursor.RotationAxis & RotationAxis.X) != 0) {
						if (KeyManager.GetButtonUp(KeyMap.RotateUp)) {
							ConstructionCursor.ThingTransform.Rotate(quaternion2 * Vector3.right, (ConstructionCursor is IMounted) ? 180f : 90f, Space.World);
							UIAudioManager.Play(RotateBlueprintHash);
						}
						if (KeyManager.GetButtonUp(KeyMap.RotateDown)) {
							ConstructionCursor.ThingTransform.Rotate(quaternion2 * Vector3.right, (ConstructionCursor is IMounted) ? (-180f) : (-90f), Space.World);
							UIAudioManager.Play(RotateBlueprintHash);
						}
					}
					if ((ConstructionCursor.RotationAxis & RotationAxis.Z) != 0) {
						if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) {
							ConstructionCursor.ThingTransform.Rotate(quaternion2 * Vector3.forward, 90f, Space.World);
							UIAudioManager.Play(RotateBlueprintHash);
						}
						if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) {
							ConstructionCursor.ThingTransform.Rotate(quaternion2 * Vector3.forward, -90f, Space.World);
							UIAudioManager.Play(RotateBlueprintHash);
						}
					}
					break;
				}
				_localGrid = ConstructionCursor.GetLocalGrid(cameraForwardGrid);
				_worldGrid = ConstructionCursor.GetWorldGrid(cameraForwardGrid);
				_worldMasterGrid = ConstructionCursor.GridController.ClampWorld(cameraForwardGrid);
				ConstructionCursor.ThingTransformPosition = _worldGrid;
				_cursorPosition = ConstructionCursor.ThingTransformPosition;
				if (newScrollData > 0f) {
					SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, _storedMothershipRotation);
				} else if (newScrollData < 0f) {
					SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, _storedMothershipRotation);
				}
				if (!_usingAutoplace) {
					if (KeyManager.GetButton(KeyMap.MouseInspect)) {
						SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, _storedMothershipRotation);
					} else {
						SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, _storedMothershipRotation);
					}
					_usingAutoplace = true;
				}
				break;
				case PlacementSnap.FaceMount: {
					Structure structure = CursorManager.CursorThing as Structure;
					if ((object)structure != null) {
						if (modePlacementExperimental) {
							Vector3 thingTransformPosition = structure.ThingTransformPosition;
							Vector3 vectorZero = Vector3.zero;
							vectorZero = RocketGrid.GetClosest(ConstructionCursor.GridController.GetWorldGridFaces(thingTransformPosition), cameraForwardGrid) - thingTransformPosition;
							Vector3 normalized = (thingTransformPosition - Parent.ThingTransformPosition).normalized;
							if (Vector3.Dot(vectorZero, normalized) <= 0f) {
								_cursorPosition = _worldGrid;
								ConstructionCursor.ThingTransformPosition = _cursorPosition;
								ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.forward, vectorZero);
								ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.up, ConstructionCursor.ThingTransform.up.FindClosestLocalAxis(structure.ThingTransform), ConstructionCursor.ThingTransform.forward);
								var refItemAngles = ConstructionCursor.ThingTransform.rotation.eulerAngles;
								var refWallAngles = structure.ThingTransform.rotation.eulerAngles;
								if ((Mathf.Abs(refItemAngles.x) > (Mathf.Abs(refWallAngles.x) + 1) && Mathf.Abs(refItemAngles.x) < (Mathf.Abs(refWallAngles.x) + 89)) ||
									(Mathf.Abs(refItemAngles.x) > (Mathf.Abs(refWallAngles.x) + 91) && Mathf.Abs(refItemAngles.x) < (Mathf.Abs(refWallAngles.x) + 179)) ||
									(Mathf.Abs(refItemAngles.x) > (Mathf.Abs(refWallAngles.x) + 181) && Mathf.Abs(refItemAngles.x) < (Mathf.Abs(refWallAngles.x) + 269)) ||
									(Mathf.Abs(refItemAngles.x) > (Mathf.Abs(refWallAngles.x) + 271) && Mathf.Abs(refItemAngles.x) < (Mathf.Abs(refWallAngles.x) + 359)) ||
									(Mathf.Abs(refItemAngles.y) > (Mathf.Abs(refWallAngles.y) + 1) && Mathf.Abs(refItemAngles.y) < (Mathf.Abs(refWallAngles.y) + 89)) ||
									(Mathf.Abs(refItemAngles.y) > (Mathf.Abs(refWallAngles.y) + 91) && Mathf.Abs(refItemAngles.y) < (Mathf.Abs(refWallAngles.y) + 179)) ||
									(Mathf.Abs(refItemAngles.y) > (Mathf.Abs(refWallAngles.y) + 181) && Mathf.Abs(refItemAngles.y) < (Mathf.Abs(refWallAngles.y) + 269)) ||
									(Mathf.Abs(refItemAngles.y) > (Mathf.Abs(refWallAngles.y) + 271) && Mathf.Abs(refItemAngles.y) < (Mathf.Abs(refWallAngles.y) + 359)) ||
									(Mathf.Abs(refItemAngles.z) > (Mathf.Abs(refWallAngles.z) + 1) && Mathf.Abs(refItemAngles.z) < (Mathf.Abs(refWallAngles.z) + 89)) ||
									(Mathf.Abs(refItemAngles.z) > (Mathf.Abs(refWallAngles.z) + 91) && Mathf.Abs(refItemAngles.z) < (Mathf.Abs(refWallAngles.z) + 179)) ||
									(Mathf.Abs(refItemAngles.z) > (Mathf.Abs(refWallAngles.z) + 181) && Mathf.Abs(refItemAngles.z) < (Mathf.Abs(refWallAngles.z) + 269)) ||
									(Mathf.Abs(refItemAngles.z) > (Mathf.Abs(refWallAngles.z) + 271) && Mathf.Abs(refItemAngles.z) < (Mathf.Abs(refWallAngles.z) + 359)))
									ConstructionCursor.ThingTransform.rotation = structure.ThingTransform.rotation;
								if (!KeyManager.GetButton(KeyMap.QuantityModifier)) {
									_usingAutoplace = false;
									if (KeyManager.GetButtonUp(KeyMap.RotateLeft)) {
										ConstructionCursor.ThingTransform.Rotate(Vector3.up, 90f, Space.Self);
										UIAudioManager.Play(RotateBlueprintHash);
									}
									if (KeyManager.GetButtonUp(KeyMap.RotateRight)) {
										ConstructionCursor.ThingTransform.Rotate(Vector3.up, -90f, Space.Self);
										UIAudioManager.Play(RotateBlueprintHash);
									}
									if (KeyManager.GetButtonUp(KeyMap.RotateUp)) {
										ConstructionCursor.ThingTransform.Rotate(Vector3.right, 90f, Space.Self);
										UIAudioManager.Play(RotateBlueprintHash);
									}
									if (KeyManager.GetButtonUp(KeyMap.RotateDown)) {
										ConstructionCursor.ThingTransform.Rotate(Vector3.right, -90f, Space.Self);
										UIAudioManager.Play(RotateBlueprintHash);
									}
									if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) {
										ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
										UIAudioManager.Play(RotateBlueprintHash);
									}
									if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) {
										ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
										UIAudioManager.Play(RotateBlueprintHash);
									}
									if (KeyManager.GetButtonUp(KeyMap.PrecisionPlace)) {
										ConstructionCursor.ThingTransform.rotation = structure.ThingTransform.rotation;
										UIAudioManager.Play(RotateBlueprintHash);
									}
								} else {
									if (newScrollData > 0f) SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
									else if (newScrollData < 0f) SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable);
									if (!_usingAutoplace) {
										SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
										_usingAutoplace = true;
									}
								}
								notBlocked = notBlocked && true;
								break;
							}
						} else if (structure.AllowMounting) {
							Vector3 thingTransformPosition = structure.ThingTransformPosition;
							Vector3 target = Vector3.zero;
							switch (structure.GetPlacementType()) {
								case PlacementSnap.Grid:
								target = RocketGrid.GetClosest(ConstructionCursor.GridController.GetWorldGridFaces(thingTransformPosition), cameraForwardGrid) - thingTransformPosition;
								break;
								case PlacementSnap.Face:
								case PlacementSnap.FaceMount: {
									Vector3 forward = structure.ThingTransform.forward;
									Vector3 a = thingTransformPosition + forward;
									Vector3 a2 = thingTransformPosition - forward;
									target = ((!(Vector3.Distance(a, Parent.ThingTransform.position) < Vector3.Distance(a2, Parent.ThingTransform.position))) ? (-forward) : forward);
									break;
								}
							}
							_cursorPosition = _worldGrid;
							ConstructionCursor.ThingTransformPosition = _cursorPosition;
							ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.forward, target);
							ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.up, ConstructionCursor.ThingTransform.up.FindClosestLocalAxis(structure.ThingTransform), ConstructionCursor.ThingTransform.forward);
							if (!KeyManager.GetButton(KeyMap.QuantityModifier)) {
								_usingAutoplace = false;
								if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) {
									ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
									UIAudioManager.Play(RotateBlueprintHash);
								}
								if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) {
									ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
									UIAudioManager.Play(RotateBlueprintHash);
								}
							} else {
								if (newScrollData > 0f) {
									SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
								} else if (newScrollData < 0f) {
									SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable);
								}
								if (!_usingAutoplace) {
									SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
									_usingAutoplace = true;
								}
							}
							canMountResult = ConstructionCursor.CanMountOnWall();
							bool canMount = canMountResult;
							notBlocked = notBlocked && canMount;
							break;
						}
						canMountResult.result = Structure.WallMountResult.InvalidNotMountable;
						canMountResult.support = structure;
						canMountResult.offending = structure;
					} else {
						canMountResult.result = Structure.WallMountResult.InvalidMissingSupport;
					}
					_cursorPosition = cameraForwardGrid;
					_worldGrid = cameraForwardGrid;
					ConstructionCursor.ThingTransformPosition = cameraForwardGrid;
					Vector3 vector = ((!(ParentHuman != null)) ? (Parent.ThingTransformPosition - ConstructionCursor.ThingTransformPosition).normalized : ((!ParentHuman.HasAuthority) ? (ParentHuman.HeadBone.position - ConstructionCursor.ThingTransformPosition).normalized : (-CameraController.Instance.MainCameraTransform.forward)));
					Vector3 vector2 = Vector3.Cross(Vector3.up, vector);
					if (RocketMath.Approximately(vector2, Vector3.zero)) {
						vector2 = Vector3.Cross(Vector3.right, vector);
					}
					Vector3 vector3 = ((Mathf.Abs(Vector3.Dot(ConstructionCursor.ThingTransform.right, vector2)) > Mathf.Abs(Vector3.Dot(ConstructionCursor.ThingTransform.up, vector2))) ? ConstructionCursor.ThingTransform.right : ConstructionCursor.ThingTransform.up);
					ConstructionCursor.ThingTransform.RotateOnto(vector3, vector3.FindClosestLocalAxis(vector2));
					ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.forward, vector, vector2);
					notBlocked = false;
					if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) {
						ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
						UIAudioManager.Play(RotateBlueprintHash);
					}
					if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) {
						ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
						UIAudioManager.Play(RotateBlueprintHash);
					}
					break;
				}
				case PlacementSnap.Face:
				if (!KeyManager.GetButton(KeyMap.QuantityModifier)) {
					_usingAutoplace = false;
					ConstructionCursor.ThingTransform.rotation = CurrentRotation;
					Quaternion quaternion = ((ConstructionCursor.Mothership == null) ? Quaternion.identity : ConstructionCursor.Mothership.ThingTransform.rotation);
					if (quaternion != _storedMothershipRotation) {
						ConstructionCursor.ThingTransform.rotation = quaternion * Quaternion.Inverse(_storedMothershipRotation) * ConstructionCursor.ThingTransform.rotation;
						_storedMothershipRotation = quaternion;
					}
					List<Vector3> worldGridFaces = ConstructionCursor.GridController.GetWorldGridFaces(_worldGrid);
					ConstructionCursor.ThingTransformPosition = _worldGrid - ConstructionCursor.ThingTransform.forward * ConstructionCursor.GridSize / 2f;
					if ((ConstructionCursor.RotationAxis & RotationAxis.X) != 0) {
						if (CurrentFace != RocketGrid.FaceInt.Up && KeyManager.GetButtonUp(KeyMap.RotateUp)) {
							if (RocketGrid.FaceInt.IsHorizontalFace(CurrentFace)) {
								Vector3 axis = Vector3.Cross(quaternion * Vector3.up, ConstructionCursor.ThingTransform.forward);
								ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis, 90f);
								CurrentFace = RocketGrid.FaceInt.Up;
							} else {
								Vector3 rhs = _worldMasterGrid - worldGridFaces[_lastHorizontalFace];
								Vector3 axis2 = Vector3.Cross(quaternion * Vector3.up, rhs);
								ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis2, 90f);
								CurrentFace = _lastHorizontalFace;
							}
							UIAudioManager.Play(RotateBlueprintHash);
						}
						if (CurrentFace != RocketGrid.FaceInt.Down && KeyManager.GetButtonUp(KeyMap.RotateDown)) {
							if (RocketGrid.FaceInt.IsHorizontalFace(CurrentFace)) {
								Vector3 axis3 = Vector3.Cross(quaternion * Vector3.up, ConstructionCursor.ThingTransform.forward);
								ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis3, -90f);
								CurrentFace = RocketGrid.FaceInt.Down;
							} else {
								Vector3 rhs2 = _worldMasterGrid - worldGridFaces[_lastHorizontalFace];
								Vector3 axis4 = Vector3.Cross(quaternion * Vector3.up, rhs2);
								ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis4, -90f);
								CurrentFace = _lastHorizontalFace;
							}
							UIAudioManager.Play(RotateBlueprintHash);
						}
					}
					if ((ConstructionCursor.RotationAxis & RotationAxis.Y) != 0 && RocketGrid.FaceInt.IsHorizontalFace(CurrentFace)) {
						if (KeyManager.GetButtonUp(KeyMap.RotateRight)) {
							UIAudioManager.Play(RotateBlueprintHash);
							ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, quaternion * Vector3.up, 90f);
							_lastHorizontalFace = (CurrentFace = RocketGrid.FaceInt.GetNextClockwiseHorizontalFace(CurrentFace));
						}
						if (KeyManager.GetButtonUp(KeyMap.RotateLeft)) {
							UIAudioManager.Play(RotateBlueprintHash);
							ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, quaternion * Vector3.up, -90f);
							_lastHorizontalFace = (CurrentFace = RocketGrid.FaceInt.GetNextAnticlockwiseHorizontalFace(CurrentFace));
						}
					}
					if ((ConstructionCursor.RotationAxis & RotationAxis.Z) != 0) {
						if (!RocketGrid.FaceInt.IsHorizontalFace(CurrentFace)) {
							if (KeyManager.GetButtonUp(KeyMap.RotateRight)) {
								ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, quaternion * Vector3.up, 90f);
								UIAudioManager.Play(RotateBlueprintHash);
							}
							if (KeyManager.GetButtonUp(KeyMap.RotateLeft)) {
								ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, quaternion * Vector3.up, -90f);
								UIAudioManager.Play(RotateBlueprintHash);
							}
						}
						if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) {
							ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
							UIAudioManager.Play(RotateBlueprintHash);
						}
						if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) {
							ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
							UIAudioManager.Play(RotateBlueprintHash);
						}
					}
					_cursorPosition = ConstructionCursor.GridController.GetWorldGridFaces(_worldGrid)[CurrentFace];
				} else {
					_localGrid = ConstructionCursor.GetLocalGrid(cameraForwardGrid);
					_worldGrid = ConstructionCursor.GetWorldGrid(cameraForwardGrid);
					_worldMasterGrid = ConstructionCursor.GridController.ClampWorld(cameraForwardGrid);
					ConstructionCursor.ThingTransformPosition = _worldGrid - ConstructionCursor.ThingTransform.forward * ConstructionCursor.GridSize / 2f;
					if (newScrollData > 0f) {
						SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, _storedMothershipRotation, _worldGrid);
					} else if (newScrollData < 0f) {
						SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, _storedMothershipRotation, _worldGrid);
					}
					if (!_usingAutoplace) {
						if (KeyManager.GetButton(KeyMap.MouseInspect)) {
							SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, _storedMothershipRotation, _worldGrid);
						} else {
							SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, _storedMothershipRotation, _worldGrid);
						}
						_usingAutoplace = true;
					}
					CurrentFace = RocketGrid.FaceInt.FaceIntFromDir(RocketGrid.GetFaceDir(ConstructionCursor.ThingTransformPosition, _worldGrid, _storedMothershipRotation));
					_cursorPosition = ConstructionCursor.GridController.GetWorldGridFaces(_worldGrid)[CurrentFace];
				}
				CurrentRotation = ConstructionCursor.ThingTransform.rotation;
				break;
			}
			SmallGrid smallGrid = ConstructionCursor as SmallGrid;
			switch (ConstructionCursor.SelectionDisplay) {
				case SelectionHighlightMethod.Grid: {
					Bounds bounds = ((!smallGrid || smallGrid.DualRegister || !(Math.Abs(smallGrid.GridSize - SmallGrid.SmallGridSize) < 0.1f)) ? ConstructionCursor.GridBounds.BoundsBig : ConstructionCursor.GridBounds.BoundsSmall);
					CursorManager.CursorSelectionTransform.localScale = bounds.size + Vector3.one * 0.05f;
					CursorManager.CursorSelectionTransform.position = _worldGrid + ConstructionCursor.ThingTransform.rotation * bounds.center;
					CursorManager.CursorSelectionTransform.rotation = ConstructionCursor.ThingTransform.rotation;
					break;
				}
				case SelectionHighlightMethod.Bounds:
				CursorManager.CursorSelectionTransform.localScale = ConstructionCursor.Bounds.size + Vector3.one * 0.05f;
				CursorManager.CursorSelectionTransform.rotation = ConstructionCursor.ThingTransform.rotation;
				CursorManager.CursorSelectionTransform.position = ConstructionCursor.ThingTransformPosition + ConstructionCursor.ThingTransform.rotation * ConstructionCursor.Bounds.center;
				break;
			}
			bool isSmallGrid = (bool)smallGrid;
			notBlocked = notBlocked && (ConstructionCursor.CanConstruct() || (modePlacementExperimental && isSmallGrid));
			if (!(IsAuthoringMode || (modePlacementExperimental && isSmallGrid))) {
				IMergeable mergeable = ConstructionCursor as IMergeable;
				if (mergeable != null) {
					Item inactiveHandItem = Parent.Slots[InactiveHand.SlotId].Occupant as Item;
					notBlocked &= mergeable.CanReplace(multiConstructor, inactiveHandItem);
				}
			}
			(ConstructionCursor as IMounted)?.Mount(ConstructionCursor.ThingTransform.position.ToGrid(ConstructionCursor.GridSize, ConstructionCursor.GridOffset));
			if (notBlocked && ConstructionCursor.StructureCollisionType == CollisionType.BlockGrid) {
				notBlocked = Vector3.SqrMagnitude(_worldGrid - Parent.RigidBody.worldCenterOfMass.GridCenter(ConstructionCursor.GridSize, ConstructionCursor.GridOffset)) > ConstructionCursor.GridSize / 2f;
				if (notBlocked) {
					foreach (DynamicThing dynamicObject in DynamicThing.DynamicObjects) {
						if (ConstructionCursor.BoundsIntersectWith(dynamicObject)) {
							if (modePlacementExperimental && isSmallGrid) {
								notBlocked = true;
								break;
							} else {
								tooltip.State = $"Construction obscured by {dynamicObject.ToTooltip()}";
								notBlocked = false;
								break;
							}
						}
					}
				}
			}
			if (KeyManager.GetMouseDown("Primary") && notBlocked) {
				_usePrimaryPosition = _cursorPosition;
				_usePrimaryRotation = ConstructionCursor.ThingTransform.rotation;
				if (ConstructionCursor.BuildPlacementTime > 0f) {
					float num = 1f;
					if (ParentHuman.Suit == null) {
						num += 0.2f;
					}
					num = Mathf.Clamp(num, 0.2f, 5f);
					ActionCoroutine = StartCoroutine(WaitUntilDone(UsePrimaryComplete, ConstructionCursor.BuildPlacementTime / num, ConstructionCursor));
				} else {
					UsePrimaryComplete();
				}
				return;
			}
			Color color = (notBlocked ? Color.green : Color.red);
			if ((bool)smallGrid) {
				List<Connection> list = ConstructionCursor.WillJoinNetwork();
				foreach (Connection openEnd in smallGrid.OpenEnds) {
					if (notBlocked) {
						openEnd.HelperRenderer.material.color = (list.Contains(openEnd) ? Color.yellow.SetAlpha(CursorAlphaConstructionHelper) : Color.green.SetAlpha(CursorAlphaConstructionHelper));
					} else {
						openEnd.HelperRenderer.material.color = Color.red.SetAlpha(CursorAlphaConstructionHelper);
					}
				}
				color = ((notBlocked && list.Count > 0) ? Color.yellow : color);
			}
			tooltip.Action = ActionStrings.Build;
			tooltip.Title = ConstructionCursor.DisplayName;
			tooltip.color = color;
			tooltip.Slider = -1f;
			if ((bool)_parentMothership) {
				tooltip.ConstructString = InterfaceStrings.MothershipConstruction;
			}
			if ((bool)multiConstructor && !multiConstructor.CanBuild(ConstructionPanel.BuildIndex)) {
				if (!string.IsNullOrEmpty(tooltip.ConstructString)) {
					tooltip.ConstructString += "\n";
				}
				tooltip.ConstructString += string.Format(InterfaceStrings.NeedMoreKit, multiConstructor.ToTooltip());
			}
			if (Settings.CurrentData.ExtendedTooltips) {
				if (isVisibleTool || (bool)multiConstructor) {
					tooltip.ShowScroll = !KeyManager.GetButton(KeyMap.QuantityModifier) && multiConstructor != null && multiConstructor.Constructables.Count > 1;
					tooltip.ShowRotate = !KeyManager.GetButton(KeyMap.QuantityModifier);
					tooltip.ShowConstructionRotate = KeyManager.GetButton(KeyMap.QuantityModifier);
					if (multiConstructor != null) {
						tooltip.BuildStateIndexMessage = string.Format(InterfaceStrings.TooltipNumberofBuildState, multiConstructor.LastSelectedIndex + 1, multiConstructor.Constructables.Count);
					}
				}
				if (ConstructionCursor.BuildStates.Count > 1) {
					Structure.BuildState buildState = ConstructionCursor.BuildStates[1];
					if ((bool)buildState.Tool.ToolEntry && (bool)buildState.Tool.ToolEntry2) {
						if (!string.IsNullOrEmpty(tooltip.ConstructString)) {
							tooltip.ConstructString += "\n";
						}
						tooltip.ConstructString += string.Format(InterfaceStrings.TooltipUpgrade2, buildState.Tool.ToolEntry.ToTooltip(), buildState.Tool.ToolEntry2.ToTooltip());
					} else if ((bool)buildState.Tool.ToolEntry) {
						if (!string.IsNullOrEmpty(tooltip.ConstructString)) {
							tooltip.ConstructString += "\n";
						}
						tooltip.ConstructString += string.Format(InterfaceStrings.TooltipUpgrade, buildState.Tool.ToolEntry.ToTooltip());
					}
				}
				switch (ConstructionCursor.PlacementType) {
					case PlacementSnap.Grid:
					if (!string.IsNullOrEmpty(tooltip.PlacementString)) {
						tooltip.PlacementString += "\n";
					}
					tooltip.PlacementString += InterfaceStrings.TooltipPlacementSnapGrid;
					if (!modePlacementExperimental && isSmallGrid) {
						tooltip.PlacementString += "\n";
						tooltip.PlacementString += string.Format(Localization.ParseTooltip(enablePlacement, false));
					} else if (isSmallGrid) {
						tooltip.PlacementString += "\n";
						tooltip.PlacementString += string.Format(Localization.ParseTooltip(disablePlacement, false));
					}
					if (!notBlocked) {
						if (GridController.World.StructureExistsOnGrid(ConstructionCursor.Position)) {
							AddErrorTooltip(InterfaceStrings.TooltipPlacementSnapFaceMountBlocked(canMountResult.offending));
						} else {
							AddErrorTooltip(InterfaceStrings.TooltipPlacementSnapFaceMountMissingSupport);
						}
					}
					break;
					case PlacementSnap.Face:
					if (!string.IsNullOrEmpty(tooltip.PlacementString)) {
						tooltip.PlacementString += "\n";
					}
					tooltip.PlacementString += InterfaceStrings.TooltipPlacementSnapFace;
					if (!modePlacementExperimental && isSmallGrid) {
						tooltip.PlacementString += "\n";
						tooltip.PlacementString += string.Format(Localization.ParseTooltip(enablePlacement, false));
					} else if (isSmallGrid) {
						tooltip.PlacementString += "\n";
						tooltip.PlacementString += string.Format(Localization.ParseTooltip(disablePlacement, false));
					}
					if (modePlacementExperimental && isSmallGrid) {
						tooltip.PlacementString += "\n";
						tooltip.PlacementString += string.Format(Localization.ParseTooltip(realignPlacement, false));
					}
					if (!notBlocked) {
						if (GridController.World.StructureExistsOnGrid(ConstructionCursor.Position)) {
							AddErrorTooltip(InterfaceStrings.TooltipPlacementSnapFaceMountBlocked(canMountResult.offending));
						} else {
							AddErrorTooltip(InterfaceStrings.TooltipPlacementSnapFaceMountMissingSupport);
						}
					}
					break;
					case PlacementSnap.FaceMount:
					if (!string.IsNullOrEmpty(tooltip.PlacementString)) {
						tooltip.PlacementString += "\n";
					}
					tooltip.PlacementString += InterfaceStrings.TooltipPlacementSnapFaceMount;
					if (!modePlacementExperimental && isSmallGrid) {
						tooltip.PlacementString += "\n";
						tooltip.PlacementString += string.Format(Localization.ParseTooltip(enablePlacement, false));
					} else if (isSmallGrid) {
						tooltip.PlacementString += "\n";
						tooltip.PlacementString += string.Format(Localization.ParseTooltip(disablePlacement, false));
					}
					if (modePlacementExperimental && isSmallGrid) {
						tooltip.PlacementString += "\n";
						tooltip.PlacementString += string.Format(Localization.ParseTooltip(realignPlacement, false));
					}
					switch (canMountResult.result) {
						case Structure.WallMountResult.InvalidMissingSupport:
						AddErrorTooltip(InterfaceStrings.TooltipPlacementSnapFaceMountMissingSupport);
						break;
						case Structure.WallMountResult.InvalidRequiresFrame:
						AddErrorTooltip(InterfaceStrings.TooltipPlacementSnapFaceMountMissingFrame);
						break;
						case Structure.WallMountResult.InvalidBlocked:
						AddErrorTooltip(InterfaceStrings.TooltipPlacementSnapFaceMountBlocked(canMountResult.offending));
						break;
						case Structure.WallMountResult.InvalidNotMountable:
						AddErrorTooltip(InterfaceStrings.TooltipPlacementSnapFaceNotMountable(canMountResult.offending));
						break;
						case Structure.WallMountResult.InvalidFacingMismatch:
						AddErrorTooltip(InterfaceStrings.TooltipPlacementSnapFaceWrongFace(canMountResult.offending));
						break;
					}
					break;
				}
				if (!_usingAutoplace) {
					tooltip.ShowRotate = true;
				} else {
					if (!string.IsNullOrEmpty(tooltip.PlacementString)) {
						tooltip.PlacementString += "\n";
					}
					tooltip.ShowScroll = false;
				}
				if (ConstructionCursor.AllowMounting) {
					if (!string.IsNullOrEmpty(tooltip.State)) {
						tooltip.State += "\n";
					}
					tooltip.State += InterfaceStrings.TooltipAllowsMounting;
				}
				if (ConstructionCursor.RotationAxis != 0 && !_usingAutoplace) {
					if (((ConstructionCursor.RotationAxis & RotationAxis.Y) != 0) || modePlacementExperimental) {
						if (!string.IsNullOrEmpty(tooltip.PlacementString)) {
							tooltip.PlacementString += "\n";
						}
						tooltip.PlacementString += InterfaceStrings.TooltipRotateLeftRight;
					}
					if (((ConstructionCursor.RotationAxis & RotationAxis.X) != 0) || modePlacementExperimental) {
						if (!string.IsNullOrEmpty(tooltip.PlacementString)) {
							tooltip.PlacementString += "\n";
						}
						tooltip.PlacementString += InterfaceStrings.TooltipRotateUpDown;
					}
					if (((ConstructionCursor.RotationAxis & RotationAxis.Z) != 0) || modePlacementExperimental) {
						if (!string.IsNullOrEmpty(tooltip.PlacementString)) {
							tooltip.PlacementString += "\n";
						}
						tooltip.PlacementString += InterfaceStrings.TooltipRollLeftRight;
					}
				}
			}
			color.a = CursorAlphaConstructionMesh;
			if ((bool)ConstructionCursor.Wireframe) {
				ConstructionCursor.Wireframe.BlueprintRenderer.material.color = color;
			} else {
				foreach (ThingRenderer renderer in ConstructionCursor.Renderers) {
					if (renderer.HasRenderer()) {
						renderer.SetColor(color);
					}
				}
			}
			CursorManager.SetSelectionColor(color.SetAlpha(CursorAlphaConstructionGrid));
			TooltipRef.HandleToolTipDisplay(tooltip);
			void AddErrorTooltip(string tooltipText) {
				if (!string.IsNullOrEmpty(tooltip.State)) {
					tooltip.State += "\n";
				}
				ref string state = ref tooltip.State;
				state = state + "<color=red>" + tooltipText + "</color>";
			}
		}
	}
}