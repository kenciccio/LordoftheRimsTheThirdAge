﻿using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.SketchGen;
using Verse;

namespace TheThirdAge
{
	public class GenStep_ScatterShrinesMedieval : GenStep_ScatterRuinsSimple
	{
		protected override bool CanScatterAt(IntVec3 c, Map map)
		{
			if (!base.CanScatterAt(c, map))
			{
				return false;
			}
			Building edifice = c.GetEdifice(map);
			return edifice != null && edifice.def.building.isNaturalRock;
		}

		protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int stackCount = 1)
		{
			var randomInRange = ShrinesCountX.RandomInRange;
			var randomInRange2 = ShrinesCountZ.RandomInRange;
			var randomInRange3 = ExtraHeightRange.RandomInRange;
			IntVec2 standardAncientShrineSize = SymbolResolver_AncientShrinesGroupMedieval.StandardAncientShrineSize;
			var num = 1;
			var num2 = (randomInRange * standardAncientShrineSize.x) + ((randomInRange - 1) * num);
			var num3 = (randomInRange2 * standardAncientShrineSize.z) + ((randomInRange2 - 1) * num);
			var num4 = num2 + 2;
			var num5 = num3 + 2 + randomInRange3;
			var rect = new CellRect(loc.x, loc.z, num4, num5);
			rect.ClipInsideMap(map);
			if (rect.Width != num4 || rect.Height != num5)
			{
				return;
			}
			foreach (IntVec3 c in rect.Cells)
			{
				List<Thing> list = map.thingGrid.ThingsListAt(c);
				for (var i = 0; i < list.Count; i++)
				{
					if (list[i].def == ThingDefOf.AncientCryptosleepCasket)
					{
						return;
					}
				}
			}
			if (!CanPlaceAncientBuildingInRange(rect, map))
			{
				return;
			}
			var resolveParams = default(RimWorld.SketchGen.ResolveParams);
			resolveParams.sketch = new Sketch();
			resolveParams.monumentSize = new IntVec2?(new IntVec2(num2, num3));
			SketchGen.Generate(SketchResolverDefOf.MonumentRuin, resolveParams).Spawn(map, rect.CenterCell, null, Sketch.SpawnPosType.Unchanged, Sketch.SpawnMode.Normal, false, false, null, false, false, delegate (SketchEntity entity, IntVec3 cell)
			{
				var result = false;
				foreach (IntVec3 b in entity.OccupiedRect.AdjacentCells)
				{
					IntVec3 c2 = cell + b;
					if (c2.InBounds(map))
					{
						Building edifice = c2.GetEdifice(map);
						if (edifice == null || !edifice.def.building.isNaturalRock)
						{
							result = true;
							break;
						}
					}
				}
				return result;
			}, null);

			var nextSignalTagID = Find.UniqueIDsManager.GetNextSignalTagID();
			var signalTag = "ancientTempleApproached-" + nextSignalTagID;
			var signalAction_Letter = (SignalAction_Letter)ThingMaker.MakeThing(ThingDefOf.SignalAction_Letter, null);
			signalAction_Letter.signalTag = signalTag;
			signalAction_Letter.letter = LetterMaker.MakeLetter("LetterLabelAncientShrineWarning".Translate(), "AncientShrineWarning".Translate(), LetterDefOf.NeutralEvent, new TargetInfo(rect.CenterCell, map, false));
			GenSpawn.Spawn(signalAction_Letter, rect.CenterCell, map);
			var rectTrigger = (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger, null);
			rectTrigger.signalTag = signalTag;
			rectTrigger.Rect = rect.ExpandedBy(1).ClipInsideMap(map);
			rectTrigger.destroyIfUnfogged = true;
			GenSpawn.Spawn(rectTrigger, rect.CenterCell, map);
		}

		private static readonly IntRange ShrinesCountX = new IntRange(1, 4);

		private static readonly IntRange ShrinesCountZ = new IntRange(1, 4);

		private static readonly IntRange ExtraHeightRange = new IntRange(0, 8);

		private const int MarginCells = 1;
	}
}
