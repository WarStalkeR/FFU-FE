#pragma warning disable CS0169
#pragma warning disable CS0626
#pragma warning disable CS0649
#pragma warning disable IDE0002
#pragma warning disable IDE1006
#pragma warning disable IDE0019

using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Util;
using Assets.Scripts.UI;
using MonoMod;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes {
	[MonoModReplace] public class PipeAnalysizer : DevicePipeMounted {
		private string _tooltip;
		public Atmosphere NetworkAtmosphere => base.SmallCell.Pipe.PipeNetwork.Atmosphere;
		private bool hasReadableAtmosphere {
			get {
				if (base.SmallCell != null && base.SmallCell.Pipe != null) {
					return base.SmallCell.Pipe.PipeNetwork?.Atmosphere != null;
				}
				return false;
			}
		}
		public bool PipeIgnited {
			get {
				if (!OnOff || !Powered || Error == 1 || !hasReadableAtmosphere) {
					return false;
				}
				return NetworkAtmosphere.Sparked;
			}
		}
		public float PipePressure {
			get {
				if (!OnOff || !Powered || Error == 1 || !hasReadableAtmosphere) {
					return -1f;
				}
				return NetworkAtmosphere.PressureGassesAndLiquids;
			}
		}
		public float PipeTemperature {
			get {
				if (!OnOff || !Powered || Error == 1 || !hasReadableAtmosphere) {
					return -1f;
				}
				return NetworkAtmosphere.Temperature;
			}
		}
		public float TotalMoles {
			get {
				if (!OnOff || !Powered || Error == 1 || !hasReadableAtmosphere) {
					return -1f;
				}
				return NetworkAtmosphere.TotalMoles;
			}
		}
		public override bool CanLogicRead(LogicType logicType) {
			switch (logicType) {
				case LogicType.Pressure:
				case LogicType.Temperature:
				case LogicType.RatioOxygen:
				case LogicType.RatioCarbonDioxide:
				case LogicType.RatioNitrogen:
				case LogicType.RatioPollutant:
				case LogicType.RatioVolatiles:
				case LogicType.RatioWater:
				case LogicType.TotalMoles:
				case LogicType.RatioNitrousOxide:
				case LogicType.Combustion:
					return true;
				default:
					return base.CanLogicRead(logicType);
			}
		}
		public override double GetLogicValue(LogicType logicType) {
			switch (logicType) {
				case LogicType.Combustion:
					if (!PipeIgnited) {
						return 0.0;
					}
					return 1.0;
				case LogicType.Pressure:
					return PipePressure;
				case LogicType.Temperature:
					return PipeTemperature;
				case LogicType.TotalMoles:
					return TotalMoles;
				case LogicType.RatioOxygen:
				case LogicType.RatioCarbonDioxide:
				case LogicType.RatioNitrogen:
				case LogicType.RatioPollutant:
				case LogicType.RatioVolatiles:
				case LogicType.RatioWater:
				case LogicType.RatioNitrousOxide:
					return GasRatio(logicType);
				default:
					return base.GetLogicValue(logicType);
			}
		}
		public new float GasRatio(LogicType logicType) {
			if (!OnOff || !Powered || Error == 1 || !hasReadableAtmosphere) {
				return -1f;
			}
			float num = 0f;
			switch (logicType) {
				case LogicType.RatioOxygen:
					num = NetworkAtmosphere.GasMixture.Oxygen.Quantity;
					break;
				case LogicType.RatioCarbonDioxide:
					num = NetworkAtmosphere.GasMixture.CarbonDioxide.Quantity;
					break;
				case LogicType.RatioNitrogen:
					num = NetworkAtmosphere.GasMixture.Nitrogen.Quantity;
					break;
				case LogicType.RatioPollutant:
					num = NetworkAtmosphere.GasMixture.Pollutant.Quantity;
					break;
				case LogicType.RatioVolatiles:
					num = NetworkAtmosphere.GasMixture.Volatiles.Quantity;
					break;
				case LogicType.RatioWater:
					num = NetworkAtmosphere.GasMixture.Water.Quantity;
					break;
				case LogicType.RatioNitrousOxide:
					num = NetworkAtmosphere.GasMixture.NitrousOxide.Quantity;
					break;
				default:
					return -1f;
			}
			return num / NetworkAtmosphere.TotalMoles;
		}
		public override PassiveTooltip GetPassiveTooltip(Collider hitCollider) {
		/// Mounted pipe analyzer to show temperature in Celsius as well.
			Tooltip.ToolTipStringBuilder.Clear();
			PassiveTooltip result = new PassiveTooltip(toDefault: true);
			if (hitCollider == null || hitCollider.transform != ThingTransform) {
				return base.GetPassiveTooltip(hitCollider);
			}
			if (!OnOff || !Powered || Error == 1) {
				return base.GetPassiveTooltip(hitCollider);
			}
			if (!hasReadableAtmosphere) {
				return base.GetPassiveTooltip(hitCollider);
			}
			PipeNetwork pipeNetwork = base.SmallCell.Pipe.PipeNetwork;
			Tooltip.AppendLine($"Pressure {pipeNetwork.Atmosphere.PressureGassesAndLiquidsInPa.ToStringPrefix("Pa", "yellow")}");
			Tooltip.AppendLine($"Temperature {pipeNetwork.Atmosphere.Temperature.ToStringPrefix("K", "yellow")} (<color=yellow>{(pipeNetwork.Atmosphere.Temperature - 273.15).ToStringRounded()}°C</color>)");
			Tooltip.AppendLine(AtmosphericsManager.DisplayGas(pipeNetwork.Atmosphere.GasMixture, pipeNetwork.Atmosphere.GasMixture.Oxygen));
			Tooltip.AppendLine(AtmosphericsManager.DisplayGas(pipeNetwork.Atmosphere.GasMixture, pipeNetwork.Atmosphere.GasMixture.Nitrogen));
			Tooltip.AppendLine(AtmosphericsManager.DisplayGas(pipeNetwork.Atmosphere.GasMixture, pipeNetwork.Atmosphere.GasMixture.CarbonDioxide));
			Tooltip.AppendLine(AtmosphericsManager.DisplayGas(pipeNetwork.Atmosphere.GasMixture, pipeNetwork.Atmosphere.GasMixture.Volatiles));
			Tooltip.AppendLine(AtmosphericsManager.DisplayGas(pipeNetwork.Atmosphere.GasMixture, pipeNetwork.Atmosphere.GasMixture.Pollutant));
			Tooltip.AppendLine(AtmosphericsManager.DisplayGas(pipeNetwork.Atmosphere.GasMixture, pipeNetwork.Atmosphere.GasMixture.Water));
			Tooltip.AppendLine(AtmosphericsManager.DisplayGas(pipeNetwork.Atmosphere.GasMixture, pipeNetwork.Atmosphere.GasMixture.NitrousOxide));
			result.Title = DisplayName;
			result.Description = Tooltip.ToolTipStringBuilder.ToString();
			return result;
		}
		public override void CheckForPipe() {
			if (GameManager.RunSimulation && !IsValidPipe() && Error == 0) {
				OnServer.Interact(base.InteractError, 1);
			} else if (GameManager.RunSimulation && IsValidPipe() && Error == 1) {
				OnServer.Interact(base.InteractError, 0);
			}
		}
		public override void OnInteractableUpdated(Interactable interactable) {
			base.OnInteractableUpdated(interactable);
			if (GameManager.RunSimulation && GameManager.GameState == GameState.Running && !IsCursor && (interactable.Action == InteractableType.OnOff || interactable.Action == InteractableType.Powered)) {
				CheckForPipe();
			}
		}
	}
}