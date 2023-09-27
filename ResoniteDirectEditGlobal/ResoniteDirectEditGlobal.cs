using HarmonyLib;
using ResoniteModLoader;
using FrooxEngine;
using System;
using FrooxEngine.ProtoFlux;
using FrooxEngine.UIX;
using Elements.Core;
using System.Collections.Generic;
using System.Linq;

namespace ResoniteDirectEditGlobal;
public class ResoniteDirectEditGlobal : ResoniteMod {
	public override string Name => "ResoniteDirectEditGlobal";
	public override string Author => "Delta";
	public override string Version => "1.2.0";
	public override string Link => "https://github.com/XDelta/ResoniteDirectEditGlobal";

	static List<string> excludeNodesNames = new() { "GlobalTo", "ToGlobal" };
	public override void OnEngineInit() {
		Harmony harmony = new Harmony("net.deltawolf.ResoniteDirectEditGlobal");
		harmony.PatchAll();
	}

	[HarmonyPatch(typeof(ProtoFluxNodeVisual), "GenerateGlobalRefElement")]
	class GenerateGlobalRefElement_Patch {
		//Make sure the GlobalValue exists before calling the GenerateRefElement
		public static void Prefix(ProtoFluxNodeVisual __instance, UIBuilder ui, Type referenceType, ISyncRef globalRef) {
			//Any that would get a variable name should be edit-able
			//any that would be a reference directly like an ibutton shouldn't be

			//included:
			//DynamicImpulses
			//Dynamic Inputs

			//excluded by complex type: 
			//Button Events
			//excluded by name:
			//GlobalToValueOutput
			//WriteValueToGlobal
			//GlobalToObjectOutput

			if (!ShouldGenerate(__instance, referenceType)) { return; }

			var globalValue = globalRef.Target ?? __instance.Node.Target.Slot.GetComponent<GlobalValue<string>>();
			//If for whatever reason it isn't set and it already has a globalvalue, this should reconnect it
			if (globalValue == null) {
				Debug("Couldn't find GlobalValue, adding one");
				globalValue = __instance.Node.Target.Slot.AttachComponent<GlobalValue<string>>();
			}
			globalRef.Target = globalValue;
		}

		public static void Postfix(ProtoFluxNodeVisual __instance, UIBuilder ui, Type referenceType, ISyncRef globalRef) {

			if (!ShouldGenerate(__instance, referenceType)) { return; }
			if (globalRef == null) {
				Debug("GlobalRef was null");
				return;
			}

			var btn = ui.Current.FindChildInHierarchy("Horizontal Layout")[0];
			var btntext = btn[0].GetComponent<Text>();
			btntext.Content.ReleaseLink(btntext.Content.ActiveLink);
			var pme = btn.AttachComponent<PrimitiveMemberEditor>();
			var tf = btn.AttachComponent<TextField>();
			var te = btn.GetComponentOrAttach<TextEditor>();

			te.Text.Target = btntext;
			var _te = (SyncRef<TextEditor>)pme.TryGetField("_textEditor");
			_te.Target = tf.Editor.Target;
			var _td = (FieldDrive<string>)pme.TryGetField("_textDrive");
			_td.Target = btntext.Content;
			var _target = (RelayRef<IField>)pme.TryGetField("_target");
			_target.Target = __instance.Node.Target.Slot.GetComponent<GlobalValue<string>>().Value;
		}

		internal static bool ShouldGenerate(ProtoFluxNodeVisual instance, Type referenceType) {
			if (referenceType != typeof(string)) {
				if (Coder.IsEnginePrimitive(referenceType)) {
					Debug($"Valid non-string primitive: {referenceType}"); //simpler types like bool, int, string, float
				} else {
					Debug($"Not valid primitive: {referenceType}, skipping generation"); //complex types like IButton
					return false;
				}
			}

			if (excludeNodesNames.Any(instance.Node.Target.Name.Contains)) {
				Debug($"Excluding {instance.Node.Target.Name}");
				return false;
			}
			return true;
		}
	}
}
