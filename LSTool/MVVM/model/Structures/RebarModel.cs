namespace LSTool.MVVM.Models
{
    public partial class RebarModel : ObservableObject
    {
        private string _name;
        public List<string> Diameters { get; set; }
        [ObservableProperty]
        private int _diameter;
        [ObservableProperty]
        private int _spacing;

        public int MinSpacing { get; set; } = 0;

        public void OnSpacingChanging(ref int value)
        {
            if (value < MinSpacing) value = MinSpacing;
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                NameChangeAction?.Invoke(this);
            }
        }
        public Action<RebarModel> NameChangeAction { get; set; }
        public int RebarSectionType { get; set; }

        public RebarModel Clone() => new RebarModel
        {
            Diameters        = Diameters != null ? new List<string>(Diameters) : null,
            Diameter         = Diameter,
            Spacing          = Spacing,
            Name             = Name,
            RebarSectionType = RebarSectionType,
            MinSpacing       = MinSpacing,
        };

    }
}
