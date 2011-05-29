using System;
using System.Collections.Generic;
using System.Text;

namespace BoneSoft.CSS {
	public static class CSSRenderer {
		public static string Render(CSSDocument css) {
			StringBuilder txt = new StringBuilder();
			txt.Append(css.ToString());
			return txt.ToString();
		}
	}
}