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
    }
}
