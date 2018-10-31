using UnityEngine;
using System.Collections;

namespace Ceto
{

    /// <summary>
    /// Interface for getting the time value used to generate the waves.
    /// Implement this interface to better control what time value is used.
    /// Useful for synchronizing clients from a server. Maybe?
    /// 
    /// Add your custom implementation to Ceto from any scripts starts function like so...
    /// 
    /// if(Ceto.Ocean.Instance != null)
    ///     Ceto.Ocean.Instance.OceanTime = new YourOceanTime();
	///
	/// You can also implement this on a game object. For example
	/// 
	/// public class YourOceanTime : MonoBehaviour, IOceanTime
	/// {
	///     
	///   public float Now { get; set; }
	///
	///   void Update()
	///   {
	///	
	///		if(Ceto.Ocean.Instance != null)
	///			Ceto.Ocean.Instance.OceanTime = this
	///
	///   }
	///
	/// }
    /// 
    /// </summary>
	public interface IOceanTime
	{

        /// <summary>
        /// The current time in seconds. 
        /// </summary>
		float Now { get; }

	}

}
