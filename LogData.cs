namespace BMS_Master_Control
{
    public class LogData
    {
		public float cell1_voltage { get; set; }
		public float cell2_voltage { get; set; }
		public float cell7_voltage { get; set; }
		public float cell8_voltage { get; set; }
		public float cell1_soc { get; set; }
		public float cell2_soc { get; set; }
		public float cell7_soc { get; set; }
		public float cell8_soc { get; set; }
		public float current { get; set; }
		public int time_elapsed { get; set; }
		public int is_charging { get; set; }
    }
}