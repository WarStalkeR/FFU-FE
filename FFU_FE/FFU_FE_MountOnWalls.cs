#pragma warning disable CS0626
#pragma warning disable IDE1006
#pragma warning disable IDE0019

using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using MonoMod;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Objects {
	public class patch_SmallGrid : SmallGrid {
		public override bool CanMountOnWall() {
			return true;
		}
	}
}

namespace Assets.Scripts.Inventory {
	public class patch_InventoryManager : InventoryManager {
		private void PlacementMode() {
			if ((!(ActiveHand.Slot.Occupant as Constructor) && !ConstructionPanel.gameObject.activeSelf && !IsAuthoringMode) || KeyManager.GetMouseDown("Secondary")) CancelPlacement();
			else {
				MultiConstructor multiConstructor = ActiveHand.Slot.Occupant as MultiConstructor;
				if (ConstructionCursor) {
					DynamicThing trackedThing = CameraController.Instance.TrackedThing;
					Transform thingTransform = trackedThing.ThingTransform;
					Vector3 vector = InputHelpers.GetCameraForwardGrid((ConstructionCursor.GridSize > 0.5f) ? 0.3f : 0.6f, ConstructionCursor.GetCursorOffset);
					_parentMothership = CursorManager.CursorThing ? CursorManager.CursorThing.GridController.ParentMothership : Mothership.GetNearbyMothership(vector);
					if (_parentMothership) vector += thingTransform.position - trackedThing.ActiveRigidbody.position;
					ConstructionCursor.GridController = (_parentMothership != null) ? _parentMothership.GridController : GridController.World;
					ConstructionCursor.Mothership = _parentMothership;
					ConstructionCursor.Position = ConstructionCursor.ThingTransform.position;
					ConstructionCursor.Rotation = ConstructionCursor.ThingTransform.rotation;
					CursorManager.SetSelection(ShowUi);
					CursorManager.Instance.CursorSelectionHighlighter.transform.rotation = Quaternion.identity;
					_cursorPosition = Vector3.zero;
					_localGrid = ConstructionCursor.GetLocalGrid(vector);
					_worldGrid = ConstructionCursor.GetWorldGrid(vector);
					_worldMasterGrid = ConstructionCursor.GridController.ClampWorld(vector, 2f, 0f);
					bool canBuildMulti = !multiConstructor || multiConstructor.CanBuild(ConstructionPanel.BuildIndex);
					switch (ConstructionCursor.PlacementType) {
						case PlacementSnap.Grid: {
							if (!KeyManager.GetButton(KeyMap.QuantityModifier)) {
								_usingAutoplace = false;
								_cursorPosition = _worldGrid;
								ConstructionCursor.ThingTransform.position = _cursorPosition;
								Quaternion quaternion = (ConstructionCursor.Mothership == null) ? Quaternion.identity : ConstructionCursor.Mothership.ThingTransform.rotation;
								if (quaternion != _storedMothershipRotation) {
									ConstructionCursor.ThingTransform.rotation = quaternion * Quaternion.Inverse(_storedMothershipRotation) * ConstructionCursor.ThingTransform.rotation;
									_storedMothershipRotation = quaternion;
								}
								if ((ConstructionCursor.RotationAxis & RotationAxis.Y) > RotationAxis.None) {
									if (KeyManager.GetButtonUp(KeyMap.RotateLeft)) ConstructionCursor.ThingTransform.Rotate(quaternion * Vector3.up, 90f, Space.World);
									if (KeyManager.GetButtonUp(KeyMap.RotateRight)) ConstructionCursor.ThingTransform.Rotate(quaternion * Vector3.up, -90f, Space.World);
								}
								if ((ConstructionCursor.RotationAxis & RotationAxis.X) > RotationAxis.None) {
									if (KeyManager.GetButtonUp(KeyMap.RotateUp)) ConstructionCursor.ThingTransform.Rotate(quaternion * Vector3.right, 90f, Space.World);
									if (KeyManager.GetButtonUp(KeyMap.RotateDown)) ConstructionCursor.ThingTransform.Rotate(quaternion * Vector3.right, -90f, Space.World);
								}
								if ((ConstructionCursor.RotationAxis & RotationAxis.Z) > RotationAxis.None) {
									if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) ConstructionCursor.ThingTransform.Rotate(quaternion * Vector3.forward, 90f, Space.World);
									if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) ConstructionCursor.ThingTransform.Rotate(quaternion * Vector3.forward, -90f, Space.World);
								}
							} else {
								_cursorPosition = ConstructionCursor.ThingTransform.position;
								if (newScrollData > 0f) SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, _storedMothershipRotation);
								else if (newScrollData < 0f) SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, _storedMothershipRotation);
								if (!_usingAutoplace) {
									SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, _storedMothershipRotation);
									_usingAutoplace = true;
								}
							}
							break;
						}
						case PlacementSnap.Face: {
							if (!KeyManager.GetButton(KeyMap.QuantityModifier)) {
								_usingAutoplace = false;
								ConstructionCursor.ThingTransform.rotation = CurrentRotation;
								Quaternion quaternion2 = (ConstructionCursor.Mothership == null) ? Quaternion.identity : ConstructionCursor.Mothership.ThingTransform.rotation;
								if (quaternion2 != _storedMothershipRotation) {
									ConstructionCursor.ThingTransform.rotation = quaternion2 * Quaternion.Inverse(_storedMothershipRotation) * ConstructionCursor.ThingTransform.rotation;
									_storedMothershipRotation = quaternion2;
								}
								List<Vector3> worldGridFaces = ConstructionCursor.GridController.GetWorldGridFaces(_worldGrid);
								ConstructionCursor.ThingTransform.position = _worldGrid - ConstructionCursor.ThingTransform.forward * ConstructionCursor.GridSize / 2f;
								if ((ConstructionCursor.RotationAxis & RotationAxis.X) > RotationAxis.None) {
									if (CurrentFace != RocketGrid.FaceInt.Up) {
										if (KeyManager.GetButtonUp(KeyMap.RotateUp)) {
											if (RocketGrid.FaceInt.IsHorizontalFace(CurrentFace)) {
												Vector3 axis = Vector3.Cross(quaternion2 * Vector3.up, ConstructionCursor.ThingTransform.forward);
												ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis, 90f);
												CurrentFace = RocketGrid.FaceInt.Up;
											} else {
												Vector3 rhs = _worldMasterGrid - worldGridFaces[_lastHorizontalFace];
												Vector3 axis2 = Vector3.Cross(quaternion2 * Vector3.up, rhs);
												ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis2, 90f);
												CurrentFace = _lastHorizontalFace;
											}
										}
									}
									if (CurrentFace != RocketGrid.FaceInt.Down) {
										if (KeyManager.GetButtonUp(KeyMap.RotateDown)) {
											if (RocketGrid.FaceInt.IsHorizontalFace(CurrentFace)) {
												Vector3 axis3 = Vector3.Cross(quaternion2 * Vector3.up, ConstructionCursor.ThingTransform.forward);
												ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis3, -90f);
												CurrentFace = RocketGrid.FaceInt.Down;
											} else {
												Vector3 rhs2 = _worldMasterGrid - worldGridFaces[_lastHorizontalFace];
												Vector3 axis4 = Vector3.Cross(quaternion2 * Vector3.up, rhs2);
												ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, axis4, -90f);
												CurrentFace = _lastHorizontalFace;
											}
										}
									}
								}
								if ((ConstructionCursor.RotationAxis & RotationAxis.Y) > RotationAxis.None) {
									if (RocketGrid.FaceInt.IsHorizontalFace(CurrentFace)) {
										if (KeyManager.GetButtonUp(KeyMap.RotateRight)) {
											ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, quaternion2 * Vector3.up, 90f);
											_lastHorizontalFace = CurrentFace = RocketGrid.FaceInt.GetNextClockwiseHorizontalFace(CurrentFace);
										}
										if (KeyManager.GetButtonUp(KeyMap.RotateLeft)) {
											ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, quaternion2 * Vector3.up, -90f);
											_lastHorizontalFace = CurrentFace = RocketGrid.FaceInt.GetNextAnticlockwiseHorizontalFace(CurrentFace);
										}
									}
								}
								if ((ConstructionCursor.RotationAxis & RotationAxis.Z) > RotationAxis.None) {
									if (!RocketGrid.FaceInt.IsHorizontalFace(CurrentFace)) {
										if (KeyManager.GetButtonUp(KeyMap.RotateRight)) ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, quaternion2 * Vector3.up, 90f);
										if (KeyManager.GetButtonUp(KeyMap.RotateLeft)) ConstructionCursor.ThingTransform.RotateAround(_worldMasterGrid, quaternion2 * Vector3.up, -90f);
									}
									if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
									if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
								}
								_cursorPosition = ConstructionCursor.GridController.GetWorldGridFaces(_worldGrid)[CurrentFace];
							} else {
								if (newScrollData > 0f) SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, _storedMothershipRotation, _worldGrid);
								else if (newScrollData < 0f) SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable, _storedMothershipRotation, _worldGrid);
								if (!_usingAutoplace) {
									SmartRotate.GetNext(ConstructionCursor as ISmartRotatable, _storedMothershipRotation, _worldGrid);
									_usingAutoplace = true;
								}
								Dir faceDir = RocketGrid.GetFaceDir(ConstructionCursor.ThingTransform.position, _worldGrid, _storedMothershipRotation);
								CurrentFace = RocketGrid.FaceInt.FaceIntFromDir(faceDir);
							}
							CurrentRotation = ConstructionCursor.ThingTransform.rotation;
							break;
						}
						case PlacementSnap.FaceMount: {
							if (CursorManager.CursorThing) {
								Structure structure = CursorManager.CursorThing as Structure;
								Wall wall = CursorManager.CursorThing as Wall;
								if (structure && structure.AllowMounting) {
									List<Vector3> worldGridFaces2 = ConstructionCursor.GridController.GetWorldGridFaces(structure.ThingTransform.position);
									Vector3 closest = RocketGrid.GetClosest(worldGridFaces2, vector);
									Vector3 vector2 = closest - structure.ThingTransform.position;
									if (Vector3.Dot(vector2, CursorManager.CursorThing.ThingTransform.position - Parent.ThingTransform.position) <= 0f) {
										_cursorPosition = _worldGrid;
										ConstructionCursor.ThingTransform.position = _cursorPosition;
										ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.forward, vector2, 0.1f);
										ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.up, ConstructionCursor.ThingTransform.up.FindClosestLocalAxis(structure.ThingTransform), ConstructionCursor.ThingTransform.forward, 0.1f);
										if (!KeyManager.GetButton(KeyMap.QuantityModifier)) {
											_usingAutoplace = false;
											if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
											if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
										} else {
											if (newScrollData > 0f) SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
											else if (newScrollData < 0f) SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable);
											if (!_usingAutoplace) {
												SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
												_usingAutoplace = true;
											}
										}
										canBuildMulti = canBuildMulti && ConstructionCursor.CanMountOnWall();
										break;
									}
								} else if (wall) {
									List<Vector3> worldGridFaces2 = ConstructionCursor.GridController.GetWorldGridFaces(wall.ThingTransform.position);
									Vector3 closest = RocketGrid.GetClosest(worldGridFaces2, vector);
									Vector3 vector2 = closest - wall.ThingTransform.position;
									if (Vector3.Dot(vector2, CursorManager.CursorThing.ThingTransform.position - Parent.ThingTransform.position) <= 0f) {
										_cursorPosition = _worldGrid;
										ConstructionCursor.ThingTransform.position = _cursorPosition;
										var refItemAngles = ConstructionCursor.ThingTransform.rotation.eulerAngles;
										var refWallAngles = wall.ThingTransform.rotation.eulerAngles;
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
											ConstructionCursor.ThingTransform.rotation = wall.ThingTransform.rotation;
										if (!KeyManager.GetButton(KeyMap.QuantityModifier)) {
											_usingAutoplace = false;
											if (KeyManager.GetButtonUp(KeyMap.RotateLeft)) ConstructionCursor.ThingTransform.Rotate(Vector3.up, 90f, Space.Self);
											if (KeyManager.GetButtonUp(KeyMap.RotateRight)) ConstructionCursor.ThingTransform.Rotate(Vector3.up, -90f, Space.Self);
											if (KeyManager.GetButtonUp(KeyMap.RotateUp)) ConstructionCursor.ThingTransform.Rotate(Vector3.right, 90f, Space.Self);
											if (KeyManager.GetButtonUp(KeyMap.RotateDown)) ConstructionCursor.ThingTransform.Rotate(Vector3.right, -90f, Space.Self);
											if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
											if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
											if (KeyManager.GetButtonUp(KeyMap.PrecisionPlace)) ConstructionCursor.ThingTransform.rotation = wall.ThingTransform.rotation;
										} else {
											if (newScrollData > 0f) SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
											else if (newScrollData < 0f) SmartRotate.GetPrevious(ConstructionCursor as ISmartRotatable);
											if (!_usingAutoplace) {
												SmartRotate.GetNext(ConstructionCursor as ISmartRotatable);
												_usingAutoplace = true;
											}
										}
										canBuildMulti = canBuildMulti && ConstructionCursor.CanMountOnWall();
										break;
									}
								}
							}
							_cursorPosition = vector;
							_worldGrid = vector;
							ConstructionCursor.ThingTransform.position = vector;
							Vector3 vector3 = Parent.ThingTransform.position - ConstructionCursor.ThingTransform.position;
							Vector3 vector4 = Vector3.Cross(Vector3.up, vector3);
							Vector3 vector5 = (Mathf.Abs(Vector3.Dot(ConstructionCursor.ThingTransform.right, vector4)) > Mathf.Abs(Vector3.Dot(ConstructionCursor.ThingTransform.up, vector4))) ? ConstructionCursor.ThingTransform.right : ConstructionCursor.ThingTransform.up;
							ConstructionCursor.ThingTransform.RotateOnto(vector5, vector5.FindClosestLocalAxis(new Vector3[] { vector4 }), 0.1f);
							ConstructionCursor.ThingTransform.RotateOnto(ConstructionCursor.ThingTransform.forward, vector3, vector4, 0.1f);
							canBuildMulti = false;
							if (KeyManager.GetButtonUp(KeyMap.RotateRollRight)) ConstructionCursor.ThingTransform.Rotate(Vector3.forward, 90f, Space.Self);
							if (KeyManager.GetButtonUp(KeyMap.RotateRollLeft)) ConstructionCursor.ThingTransform.Rotate(Vector3.forward, -90f, Space.Self);
							break;
						}
					}
					SmallGrid smallGrid = ConstructionCursor as SmallGrid;
					if (!KeyManager.GetButton(KeyMap.QuantityModifier)) {
						SelectionHighlightMethod selectionDisplay = ConstructionCursor.SelectionDisplay;
						if (selectionDisplay != SelectionHighlightMethod.Grid) {
							if (selectionDisplay == SelectionHighlightMethod.Bounds) {
								CursorManager.CursorSelectionTransform.localScale = ConstructionCursor.Bounds.size + Vector3.one * 0.05f;
								CursorManager.CursorSelectionTransform.rotation = ConstructionCursor.ThingTransform.rotation;
								CursorManager.CursorSelectionTransform.position = ConstructionCursor.ThingTransform.position + ConstructionCursor.ThingTransform.rotation * ConstructionCursor.Bounds.center;
							}
						} else {
							Bounds bounds;
							if (smallGrid && !smallGrid.DualRegister && Math.Abs(smallGrid.GridSize - SmallGrid.SmallGridSize) < 0.1f) bounds = ConstructionCursor.GridBounds.BoundsSmall;
							else bounds = ConstructionCursor.GridBounds.BoundsBig;
							CursorManager.CursorSelectionTransform.localScale = bounds.size + Vector3.one * 0.05f;
							CursorManager.CursorSelectionTransform.position = _worldGrid + ConstructionCursor.ThingTransform.rotation * bounds.center;
							CursorManager.CursorSelectionTransform.rotation = ConstructionCursor.ThingTransform.rotation;
						}
					}
					canBuildMulti = canBuildMulti && (ConstructionCursor.CanConstruct() || KeyManager.GetButton(KeyMap.PrecisionPlace));
					if (!IsAuthoringMode) {
						IMergeable mergeable = ConstructionCursor as IMergeable;
						if (mergeable != null) {
							Item inactiveHandItem = Parent.Slots[InactiveHand.SlotId].Occupant as Item;
							canBuildMulti &= mergeable.CanReplace(multiConstructor, inactiveHandItem);
						}
					}
					if (canBuildMulti && ConstructionCursor.StructureCollisionType == CollisionType.BlockGrid) {
						canBuildMulti = Vector3.SqrMagnitude(_worldGrid - Parent.RigidBody.worldCenterOfMass.GridCenter(ConstructionCursor.GridSize, ConstructionCursor.GridOffset)) > ConstructionCursor.GridSize / 2f;
						if (canBuildMulti) {
							foreach (DynamicThing dynamicThing in DynamicThing.DynamicObjects) {
								if (ConstructionCursor.BoundsIntersectWith(dynamicThing)) {
									Tooltip.State = string.Format("Construction obscured by {0}", dynamicThing.ToTooltip());
									canBuildMulti = false;
									break;
								}
							}
						}
					}
					if (KeyManager.GetMouseDown("Primary") && canBuildMulti) {
						_usePrimaryPosition = _cursorPosition;
						_usePrimaryGridPosition = ConstructionCursor.GridController.WorldToLocalGrid(_usePrimaryPosition, 2f, 0f);
						_usePrimaryGridOffset = _cursorPosition - ConstructionCursor.GridController.LocalToWorld(_usePrimaryGridPosition);
						_usePrimaryRotation = ConstructionCursor.ThingTransform.rotation;
						if (ConstructionCursor.BuildPlacementTime > 0f) ActionCoroutine = StartCoroutine(WaitUntilDone(new DelegateEvent(UsePrimaryComplete), ConstructionCursor.BuildPlacementTime, ConstructionCursor));
						else UsePrimaryComplete();
					} else {
						Color color = canBuildMulti ? Color.green : Color.red;
						if (smallGrid) {
							List<Connection> list = ConstructionCursor.WillJoinNetwork();
							foreach (Connection connection in smallGrid.OpenEnds) {
								if (canBuildMulti) connection.HelperRenderer.material.color = list.Contains(connection) ? Color.yellow.SetAlpha(CursorAlphaConstructionHelper) : Color.green.SetAlpha(CursorAlphaConstructionHelper);
								else connection.HelperRenderer.material.color = Color.red.SetAlpha(CursorAlphaConstructionHelper);
							}
							color = (canBuildMulti && list.Count > 0) ? Color.yellow : color;
						}
						Tooltip.Action = ActionStrings.Build;
						Tooltip.Title = ConstructionCursor.DisplayName;
						Tooltip.WorldPosition = ConstructionCursor.Bounds.center + ConstructionCursor.ThingTransform.position;
						Tooltip.Color = color;
						Tooltip.Slider = -1f;
						Tooltip.State = string.Empty;
						if (_parentMothership) Tooltip.State = InterfaceStrings.MothershipConstruction;
						if (multiConstructor && !multiConstructor.CanBuild(ConstructionPanel.BuildIndex)) {
							if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
							Tooltip.State += string.Format(InterfaceStrings.NeedMoreKit, multiConstructor.ToTooltip());
						}
						bool extendedTooltips = Settings.CurrentData.ExtendedTooltips;
						if (extendedTooltips) {
							if (multiConstructor && multiConstructor.Constructables.Count > 1 && !_usingAutoplace) {
								if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
								Tooltip.State += string.Format(InterfaceStrings.MulticonstructorItem, multiConstructor.LastSelectedIndex + 1, multiConstructor.Constructables.Count);
							}
							if (ConstructionCursor.BuildStates.Count > 1) {
								Structure.BuildState buildState = ConstructionCursor.BuildStates[1];
								if (buildState.Tool.ToolEntry && buildState.Tool.ToolEntry2) {
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									Tooltip.State += string.Format(InterfaceStrings.TooltipUpgrade2, buildState.Tool.ToolEntry.ToTooltip(), buildState.Tool.ToolEntry2.ToTooltip());
								} else {
									if (buildState.Tool.ToolEntry) {
										if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
										Tooltip.State += string.Format(InterfaceStrings.TooltipUpgrade, buildState.Tool.ToolEntry.ToTooltip());
									}
								}
							}
							switch (ConstructionCursor.PlacementType) {
								case PlacementSnap.Grid: {
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									Tooltip.State += InterfaceStrings.TooltipPlacementSnapGrid;
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									if (!_usingAutoplace) Tooltip.State += string.Format(Localization.ParseTooltip("Hold {KEY:QuantityModifier} to enable <color=green>Autoplace</color> mode", false));
									else Tooltip.State += string.Format("Using <color=green>Autoplace</color> Scroll <color=red>Mouse Wheel</color> to select rotation options.");
									break;
								}
								case PlacementSnap.Face: {
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									Tooltip.State += InterfaceStrings.TooltipPlacementSnapFace;
									break;
								}
								case PlacementSnap.FaceMount: {
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									Tooltip.State += InterfaceStrings.TooltipPlacementSnapFaceMount;
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									Tooltip.State += string.Format(Localization.ParseTooltip("Placement is also allowed on <color=green>walls</color> to a certain extent. This is an <color=yellow>experimental</color> feature!", false));
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									Tooltip.State += string.Format(Localization.ParseTooltip("Use {KEY:PrecisionPlace} to <color=green>re-align</color> current object to the mountable surface, if it isn't aligned properly.", false));
									break;
								}
							}
							if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
							Tooltip.State += string.Format(Localization.ParseTooltip("Hold {KEY:PrecisionPlace} to <color=green>force</color> current object to build in the restricted place/location.", false));
							bool allowMounting = ConstructionCursor.AllowMounting;
							if (allowMounting) {
								if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
								Tooltip.State += InterfaceStrings.TooltipAllowsMounting;
							}
							switch (ConstructionCursor.PlacementType) {
								case PlacementSnap.Grid:
								case PlacementSnap.Face: {
									if (ConstructionCursor.RotationAxis != RotationAxis.None && !_usingAutoplace) {
										if ((ConstructionCursor.RotationAxis & RotationAxis.Y) > RotationAxis.None) {
											if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
											Tooltip.State += InterfaceStrings.TooltipRotateLeftRight;
										}
										if ((ConstructionCursor.RotationAxis & RotationAxis.X) > RotationAxis.None) {
											if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
											Tooltip.State += InterfaceStrings.TooltipRotateUpDown;
										}
										if ((ConstructionCursor.RotationAxis & RotationAxis.Z) > RotationAxis.None) {
											if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
											Tooltip.State += InterfaceStrings.TooltipRollLeftRight;
										}
									}
									break;
								}
								case PlacementSnap.FaceMount: {
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									Tooltip.State += InterfaceStrings.TooltipRotateLeftRight;
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									Tooltip.State += InterfaceStrings.TooltipRotateUpDown;
									if (!string.IsNullOrEmpty(Tooltip.State)) Tooltip.State += "\n";
									Tooltip.State += InterfaceStrings.TooltipRollLeftRight;
									break;
								}
							}
						}
						color.a = CursorAlphaConstructionMesh;
						if (ConstructionCursor.Wireframe) {
							ConstructionCursor.Wireframe.BlueprintRenderer.material.color = color;
						} else {
							foreach (ThingRenderer thingRenderer in ConstructionCursor.Renderers) {
								if (!(thingRenderer == null || thingRenderer.Renderer == null)) {
									thingRenderer.Renderer.enabled = thingRenderer.Enabled;
									foreach (Material material in thingRenderer.Renderer.materials) {
										material.color = color;
									}
								}
							}
						}
						CursorManager.SetSelection(color.SetAlpha(CursorAlphaConstructionGrid));
					}
				}
			}
		}
		[MonoModIgnore] private void UsePrimaryComplete() {}
		[MonoModIgnore] private Mothership _parentMothership;
		[MonoModIgnore] private Vector3 _cursorPosition;
		[MonoModIgnore] private Grid3 _localGrid;
		[MonoModIgnore] private Vector3 _worldGrid;
		[MonoModIgnore] private Vector3 _worldMasterGrid;
		[MonoModIgnore] private bool _usingAutoplace;
		[MonoModIgnore] private static Quaternion _storedMothershipRotation;
		[MonoModIgnore] private Vector3 _usePrimaryPosition;
		[MonoModIgnore] private Grid3 _usePrimaryGridPosition;
		[MonoModIgnore] private Vector3 _usePrimaryGridOffset;
		[MonoModIgnore] private Quaternion _usePrimaryRotation;
		[MonoModIgnore] private int _lastHorizontalFace;
	}
}