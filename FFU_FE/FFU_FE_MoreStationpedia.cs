#pragma warning disable CS0626
#pragma warning disable CS0649
#pragma warning disable IDE0002
#pragma warning disable IDE1006
#pragma warning disable IDE0019

/*
using System;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;
using MonoMod;

namespace Assets.Scripts.UI {
    public class patch_Stationpedia : Stationpedia {
		[MonoModIgnore] private void AddCreatedReagent(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddCreatedGases(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddConstructedBy(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddModeStrings(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddSlotInfo(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddLogicModeInfo(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddLogicTypeInfo(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddLogicSlotTypeInfo(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void PopulateStructureTiers(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddConstructs(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddMadeBy(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddBuildStates(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddCreates(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddResources(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddUsedIn(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddDevice(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddGasCanister(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddAtmospherics(Thing prefab, ref StationpediaPage page) { }
		[MonoModIgnore] private void AddNutrition(Thing prefab, ref StationpediaPage page) { }
		[MonoModReplace] private void PopulateThingPages() {
		/// Show more entry properties info based on type.
			StationpediaPage defPage = GetPage("ThingTemplate");
			StationpediaPage thermalPage = GetPage("ThingThermalTemplate");
			ThingSlotHeader = GetPage("ThingSlotHeaderTemplate");
			ThingSlotItem = GetPage("ThingSlotItemTemplate");
			CreatorHeader = GetPage("CreatorsHeaderTemplate");
			CreatorItem = GetPage("CreatorTemplate");
			LogicTypeHeader = GetPage("LogicTypeHeaderTemplate");
			LogicTypeItem = GetPage("LogicTypeTemplate");
			CreatedHeader = GetPage("CreatedHeaderTemplate");
			ConstructedHeader = GetPage("ConstructedHeaderTemplate");
			ConstructedByHeader = GetPage("ConstructedByHeaderTemplate");
			ModeStringHeader = GetPage("ModeStringHeaderTemplate");
			ModeStringItem = GetPage("ModeStringItemTemplate");
			CreatedReagent = GetPage("CreatedReagentTemplate");
			ThingStack = GetPage("ThingStackTemplate");
			ThingNutrition = GetPage("ThingNutritionTemplate");
			IceMelting = GetPage("IceMeltingTemplate");
			DevicePage = GetPage("DeviceTemplate");
			GasCanisterPage = GetPage("GasCanisterTemplate");
			AtmosphericsPage = GetPage("AtmosphericsTemplate");
			NutritionPage = GetPage("NutritionPageTemplate");
			CreatedGases = GetPage("CreatedGasesTemplate");
			ThingTransmissableTemplate = GetPage("ThingTransmissibleTemplate");
			TransmitTemplate = GetPage("LogicTransmitHeaderTemplate");
			TierListHeader = GetPage("TierListHeaderTemplate");
			DataHandler.HandleThingPageOverrides();
			for (int i = 0; i < Prefab.AllPrefabs.Count; i++) {
				Thing thing = Prefab.AllPrefabs[i];
				StationpediaPage wikiPage = new StationpediaPage($"Thing{thing.PrefabName}", thing.DisplayName);
				_ = string.Empty;
				_ = string.Empty;
				DataHandler.HiddenInPedia.TryGetValue(thing.PrefabName, out var value);
				if (thing.HideInStationpedia || value) continue;
				IQuantity quantity = thing as IQuantity;
				if (quantity != null && ThingStack != null) {
					try { wikiPage.StackSizeText = quantity.GetMaxQuantity.ToString();} 
					catch (FormatException ex) { Debug.LogError("There was an error with text " + ThingStack.Parsed + " " + ex.Message); }
				}
				INutrition nutrition = thing as INutrition;
				if (nutrition != null && ThingNutrition != null) {
					float useAmount = quantity?.GetMaxQuantity ?? 1f;
					if (nutrition.Nutrition(useAmount) > 0f) {
						try { wikiPage.StackSizeText = string.Format(ThingNutrition.Parsed, nutrition.Nutrition(useAmount).ToString()); } 
						catch (FormatException ex2) { Debug.LogError("There was an error with text " + ThingNutrition.Parsed + " " + ex2.Message); }
					}
				}
				Ice ice = thing as Ice;
				if (ice != null && IceMelting != null) {
					try { wikiPage.SpecificHeatText = string.Format("{0} ({1} <sup>o</sup>C))", ice.MeltTemperature.ToStringPrefix("K"), RocketMath.KelvinToCelsius(ice.MeltTemperature)); } 
					catch (FormatException ex3) { Debug.LogError("There was an error with text " + IceMelting.Parsed + " " + ex3.Message); }
				}
				if (defPage == null) continue;
				try {
					wikiPage.PrefabHashText = thing.PrefabHash;
					wikiPage.PaintableText = ((thing.PaintableMaterial != null) ? "Yes" : "No");
					wikiPage.Description = Localization.ParseHelpText(Localization.GetThingDescription(thing.PrefabName));
					if (thing is ITransmitable && ThingTransmissableTemplate != null) {
						try { wikiPage.Description += string.Format(ThingTransmissableTemplate.Parsed); } 
						catch (FormatException ex4) { Debug.LogError("There was an error with text " + ThingTransmissableTemplate.Parsed + " " + ex4.Message); }
					}
				} catch (FormatException ex5) {
					Debug.LogError("There was an error with text " + defPage.Parsed + " " + ex5.Message);
				}
				AddCreatedReagent(thing, ref wikiPage);
				AddCreatedGases(thing, ref wikiPage);
				AddConstructedBy(thing, ref wikiPage);
				AddModeStrings(thing, ref wikiPage);
				if (thing.InternalAtmosphere != null) wikiPage.PressureBreakText = $"{thing.InternalAtmosphere.Volume}L";
				Cable cable = thing as Cable;
				if ((object)cable != null) wikiPage.CableBreakText = $"{cable.MaxVoltage}W";
				if (thing is Pipe) wikiPage.PressureBreakText = $"{Pipe.MaxPressureDelta}kpa";
				try {
					wikiPage.ShatterTempText = $"{thing.ShatterTemperature.ToStringPrefix("K")} ({RocketMath.KelvinToCelsius(thing.ShatterTemperature)}<sup>o</sup>C)";
					wikiPage.AutoIgnitionText = thing.AutoignitionTemperature < 0f ? "" : $"{thing.AutoignitionTemperature.ToStringPrefix("K")} ({RocketMath.KelvinToCelsius(thing.AutoignitionTemperature)}<sup>o</sup>C)";
					wikiPage.FlashpointText = thing.FlashpointTemperature < 0f ? "" : $"{thing.FlashpointTemperature.ToStringPrefix("K")} ({RocketMath.KelvinToCelsius(thing.FlashpointTemperature)}<sup>o</sup>C)";
				} catch (FormatException ex6) {
					Debug.LogError("There was an error with text " + thermalPage.Parsed + " " + ex6.Message);
				}
				AddSlotInfo(thing, ref wikiPage);
				AddLogicModeInfo(thing, ref wikiPage);
				AddLogicTypeInfo(thing, ref wikiPage);
				AddLogicSlotTypeInfo(thing, ref wikiPage);
				PopulateStructureTiers(thing, ref wikiPage);
				AddConstructs(thing, ref wikiPage);
				AddMadeBy(thing, ref wikiPage);
				AddBuildStates(thing, ref wikiPage);
				AddCreates(thing, ref wikiPage);
				AddResources(thing, ref wikiPage);
				AddUsedIn(thing, ref wikiPage);
				AddDevice(thing, ref wikiPage);
				AddGasCanister(thing, ref wikiPage);
				AddAtmospherics(thing, ref wikiPage);
				AddNutrition(thing, ref wikiPage);
				wikiPage.ParsePage();
				Register(wikiPage);
			}
		}
	}
}
*/