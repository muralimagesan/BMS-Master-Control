namespace BMS_Master_Control
{
    public class StatusData
    {
        public float cell1_voltage { get; set; }
        public float cell2_voltage { get; set; }
        public float cell7_voltage { get; set; }
        public float cell8_voltage { get; set; }
		public float cell1_soc { get; set; }
		public float cell2_soc{ get; set; }
		public float cell7_soc { get; set; }
		public float cell8_soc { get; set; }
		public float cell1_temp { get; set; }
		public float cell2_temp { get; set; }
		public float cell7_temp { get; set; }
		public float cell8_temp { get; set; }
        public float current { get; set; }
        public float pack_voltage { get; set; }
        public int time_elapsed { get; set; }
        public bool is_charging { get; set;  }
    }
}