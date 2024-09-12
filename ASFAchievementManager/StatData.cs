namespace ASFAchievementManager;
public class StatData {
	public uint StatNum { get; set; }
	public int BitNum { get; set; }
	public bool IsSet { get; set; }
	public bool Restricted { get; set; }
	public uint Dependancy { get; set; }
	public uint DependancyValue { get; set; }
	public string? DependancyName { get; set; }
	public string? Name { get; set; }
	public uint StatValue { get; set; }
}
