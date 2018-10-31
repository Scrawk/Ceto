using System;
using System.Collections;

namespace Ceto.Common.Threading.Scheduling
{
	public interface ICoroutine
	{

		void RunCoroutine(IEnumerator e);

	}
}
