using System;
using System.Collections.Generic;
using System.Text;

namespace ASFAchievementManager {
	class StatData {
		public uint StatNum { get; set; }
		public int BitNum { get; set; }
		public bool IsSet { get; set; }
		public bool Restricted { get; set; }
		public uint Dependancy { get; set; }
		public uint DependancyValue { get; set; }
		public string? DependancyName { get; set; }
		public string? Name { get; set; }
	}
}
