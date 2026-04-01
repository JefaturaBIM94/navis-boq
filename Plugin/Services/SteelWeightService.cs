using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class SteelWeightService : ISteelWeightService
    {
        private readonly IModelCategoryAliasService _categoryAliasService;

        private static readonly string[] SteelKeywords =
        {
            "steel", "acero", "metal", "metalic", "metallic", "w shape", "hss", "pipe",
            "ub-", "uc-", "joist", "angle", "round bar"
        };

        private static readonly string[] ConcreteKeywords =
        {
            "concrete", "concreto", "hormigon", "masonry"
        };

        private static readonly Regex ReNumUnit = new Regex(
            @"(?<num>[-+]?\d+(?:[.,]\d+)?)\s*(?<unit>kg|g|t|ton|tons)?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public SteelWeightService(IModelCategoryAliasService categoryAliasService)
        {
            _categoryAliasService = categoryAliasService ?? throw new ArgumentNullException(nameof(categoryAliasService));
        }

        public bool IsSteelCandidate(ElementSnapshot snapshot)
        {
            if (snapshot == null) return false;

            var normalizedCategory = _categoryAliasService.NormalizeStructuralCategory(snapshot.Category, snapshot.CategoryId);

            bool isTargetCategory =
                _categoryAliasService.IsStructuralFraming(normalizedCategory, snapshot.CategoryId) ||
                _categoryAliasService.IsStructuralColumn(normalizedCategory, snapshot.CategoryId);

            if (!isTargetCategory)
                return false;

            var mat = (snapshot.TypeMaterial ?? snapshot.Material ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(mat))
            {
                if (ConcreteKeywords.Any(k => mat.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                    return false;

                if (SteelKeywords.Any(k => mat.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;
            }

            return true;
        }

        public SteelRow BuildSteelRow(ElementSnapshot snapshot)
        {
            if (snapshot == null) return null;

            double pesoKg = 0;
            string metodo = "N/D";

            double effectiveLength = snapshot.CutLengthM > 0 ? snapshot.CutLengthM : snapshot.LengthM;

            double effectiveWeight = 0;
            if (snapshot.NominalWeightKgm > 0)
                effectiveWeight = snapshot.NominalWeightKgm;
            else if (snapshot.LinearWeightKgm > 0)
                effectiveWeight = snapshot.LinearWeightKgm;

            // Método principal 2025+
            if (effectiveWeight > 0 && effectiveLength > 0)
            {
                pesoKg = effectiveWeight * effectiveLength;
                metodo = "2025+";
            }
            // Nuevo fallback para IFC / DirectShape con VDC_WEIGHT textual
            else if (TryReadCustomWeightKg(snapshot, out var customWeightKg) && customWeightKg > 0)
            {
                pesoKg = customWeightKg;
                metodo = "CustomWeight";
            }
            else if (snapshot.VolumeM3 > 0)
            {
                pesoKg = snapshot.VolumeM3 * 7850.0;
                metodo = "Vol×ρ";
            }

            return new SteelRow
            {
                Nivel = snapshot.Level ?? "Sin nivel",
                Categoria = _categoryAliasService.NormalizeStructuralCategory(snapshot.Category, snapshot.CategoryId),
                Familia = string.IsNullOrWhiteSpace(snapshot.Family) ? "" : snapshot.Family.Trim(),
                Tipo = string.IsNullOrWhiteSpace(snapshot.Type) ? "" : snapshot.Type.Trim(),
                NominalWeight = Math.Round(
                    snapshot.NominalWeightKgm > 0 ? snapshot.NominalWeightKgm : snapshot.LinearWeightKgm,
                    4),
                SectionName = snapshot.SectionName ?? "",
                SectionShape = snapshot.SectionShape ?? "",
                CodeName = snapshot.CodeName ?? "",
                MaterialEst = !string.IsNullOrWhiteSpace(snapshot.TypeMaterial)
                    ? snapshot.TypeMaterial
                    : (snapshot.Material ?? ""),
                Length = Math.Round(effectiveLength, 4),
                Volume = Math.Round(snapshot.VolumeM3, 4),
                PesoKg = Math.Round(pesoKg, 2),
                Metodo = metodo,
                ElemId = snapshot.ElementId ?? "",
                Mark = snapshot.Mark ?? ""
            };
        }

        private static bool TryReadCustomWeightKg(ElementSnapshot snapshot, out double weightKg)
        {
            weightKg = 0;

            // Ajusta este campo cuando metas el dato en SnapshotService.
            // La idea es que aquí llegue algo como "0.64 kg".
            var raw = snapshot.CustomWeightRaw ?? "";

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var m = ReNumUnit.Match(raw.Trim());
            if (!m.Success)
                return false;

            var numRaw = m.Groups["num"].Value.Replace(",", ".");
            if (!double.TryParse(numRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return false;

            var unit = (m.Groups["unit"].Value ?? "").Trim().ToLowerInvariant();

            switch (unit)
            {
                case "":
                case "kg":
                    weightKg = value;
                    return true;
                case "g":
                    weightKg = value / 1000.0;
                    return true;
                case "t":
                case "ton":
                case "tons":
                    weightKg = value * 1000.0;
                    return true;
                default:
                    return false;
            }
        }
    }
}