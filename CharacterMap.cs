using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace VectorCharMap
{
    public class CharacterMap
    {
        private Dictionary<Typeface, IEnumerable<int>> Characters { get; set; }

        private FontFamily currentFontFamily;
        public FontWeight CurrentFontWeight { get; set; }
        public FontStyle CurrentFontStyle { get; set; }
        public FontStretch CurrentFontStretch { get; set; }

        public List<FontWeight> FontWeights { get; }
        public List<FontStyle> FontStyles { get; }
        public List<FontStretch> FontStretches { get; }

        public CharacterMap(FontFamily fontFamily)
        {
            Characters = new Dictionary<Typeface, IEnumerable<int>>();
            currentFontFamily = fontFamily;

            this.FontWeights = new List<FontWeight>();
            this.FontStyles = new List<FontStyle>();
            this.FontStretches = new List<FontStretch>();

            foreach (var familyTypeface in fontFamily.FamilyTypefaces)
            {
                var typeface = new Typeface(fontFamily,
                                            familyTypeface.Style,
                                            familyTypeface.Weight,
                                            familyTypeface.Stretch);

                if (typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface))
                {
                    Characters[typeface] = glyphTypeface.CharacterToGlyphMap.Select(gm => gm.Key);
                }

                if (!this.FontWeights.Contains(typeface.Weight))
                {
                    this.FontWeights.Add(typeface.Weight);
                }

                if (!this.FontStyles.Contains(typeface.Style))
                {
                    this.FontStyles.Add(typeface.Style);
                }

                if (!this.FontStretches.Contains(typeface.Stretch))
                {
                    this.FontStretches.Add(typeface.Stretch);
                }
            }
        }

        public IEnumerable<int> RequestCharacters(FontWeight fontWeight, FontStyle fontStyle, FontStretch fontStretch)
        {
            var typeface = new Typeface(currentFontFamily,
                                        fontStyle,
                                        fontWeight,
                                        fontStretch);

            if (this.Characters.TryGetValue(typeface, out IEnumerable<int> characters))
            {
                return characters;
            }

            return new List<int>();
        }

        public IEnumerable<int> RequestCurrentMapCharacters()
        {
            return RequestCharacters(CurrentFontWeight, CurrentFontStyle, CurrentFontStretch);
        }
    }
}