using MudBlazor;

namespace MyPoS.Shared
{
    /// <summary>
    /// Palet MudBlazor yang dikunci pada token yang sama dengan wwwroot/css/mypos.css.
    /// Warna dijaga di satu tempat supaya komponen bawaan MudBlazor dan gaya khusus
    /// aplikasi tidak pernah berselisih nada.
    /// </summary>
    public static class AppTheme
    {
        public const string Paper = "#F4F0E9";
        public const string PaperDark = "#131110";
        public const string Card = "#FFFFFF";
        public const string CardDark = "#1C1917";
        public const string Brand = "#B3382B";
        public const string BrandDark = "#E27A6B";
        public const string Leaf = "#1F6F5C";
        public const string LeafDark = "#5FB69B";
        public const string Turmeric = "#B5811A";
        public const string TurmericDark = "#DCA94F";

        public static MudTheme Build(string accent = Brand) => new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = accent,
                Secondary = Leaf,
                Tertiary = Turmeric,
                Success = Leaf,
                Warning = Turmeric,
                Error = "#B3382B",
                Info = "#2C5F8A",

                Background = Paper,
                BackgroundGray = "#EDE7DD",
                Surface = Card,
                AppbarBackground = Card,
                AppbarText = "#1A1714",
                DrawerBackground = Card,
                DrawerText = "#443C34",
                DrawerIcon = "#756B5F",

                TextPrimary = "#1A1714",
                TextSecondary = "#756B5F",
                TextDisabled = "#A79C8E",
                ActionDefault = "#756B5F",
                ActionDisabled = "#A79C8E",

                Divider = "#E2D9CB",
                DividerLight = "#EFE9DF",
                LinesDefault = "#E2D9CB",
                LinesInputs = "#CFC3B0",
                TableLines = "#E2D9CB",
                TableStriped = "#FAF7F1",
                TableHover = "#FAF7F1"
            },
            PaletteDark = new PaletteDark
            {
                Primary = accent == Brand ? BrandDark : accent,
                Secondary = LeafDark,
                Tertiary = TurmericDark,
                Success = LeafDark,
                Warning = TurmericDark,
                Error = "#E27A6B",
                Info = "#7FB3DE",

                Background = PaperDark,
                BackgroundGray = "#0E0C0B",
                Surface = CardDark,
                AppbarBackground = CardDark,
                AppbarText = "#F3EEE7",
                DrawerBackground = CardDark,
                DrawerText = "#D2C8BC",
                DrawerIcon = "#9A8F82",

                TextPrimary = "#F3EEE7",
                TextSecondary = "#9A8F82",
                TextDisabled = "#6E645A",
                ActionDefault = "#9A8F82",
                ActionDisabled = "#6E645A",

                Divider = "#332C26",
                DividerLight = "#221E1B",
                LinesDefault = "#332C26",
                LinesInputs = "#463D35",
                TableLines = "#332C26",
                TableStriped = "#221E1B",
                TableHover = "#221E1B"
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "10px",
                DrawerWidthLeft = "248px"
            }
        };
    }
}
