using CommunityToolkit.Mvvm.ComponentModel;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.models
{
    public abstract partial class RebarBaseInfo : ObservableObject
    {
        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value == 1 ? 2 : value;
                //for (var i = 0; i < _quantity; i++)
                //{
                //    Hooks[i] = false;
                //}
                HasHorizontalHook = false;
                OnPropertyChanged();
                QuantityChange?.Invoke();
                QtyInstall = Quantity;
            }
        }
        public int QtyInstall { get; set; }
        public Action QuantityChange { get; set; }
        //[ObservableProperty]
        //private string _diameter;

        private string _diameter;
        public string Diameter
        {
            get => _diameter;
            set
            {
                if(value != _diameter)
                {
                    _diameter = value;
                    OnPropertyChanged();
                }
            }
        }

        public long HostId;
        public int RebarBeamType { get; set; }
        /// <summary>
        /// Định nghĩa thứ tự hook trên cùng nối với dưới cùng
        /// </summary>
        //public Dictionary<int, bool> Hooks { get; set; } = new();
        public Dictionary<int, bool> Hooks2 { get; set; }
        /// <summary>
        /// Có hook theo chiều ngang
        /// </summary>
        public bool HasHorizontalHook { get; set; }
    }
}


