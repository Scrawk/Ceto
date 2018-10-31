using UnityEngine;
using System;
using System.Collections.Generic;


namespace Ceto.Common.Unity.Utility
{
	
	/// <summary>
	/// Allows a list of functions to be added to a gameobject.
	/// When some event occurs each function is called.
	/// Allows for some custom code to run before event.
	/// </summary>
	public abstract class NotifyOnEvent : MonoBehaviour 
	{
		
		/// <summary>
		/// Globally disable/enable the notification.
		/// Used to prevent a recursive notifications
		/// from happening.
		/// </summary>
		public static bool Disable;
		
		/// <summary>
		/// 
		/// </summary>
		interface INotify
		{
			
		}
		
		/// <summary>
		/// Notification with a action.
		/// </summary>
		class Notify : INotify
		{
			public Action<GameObject> action;
		}
		
		/// <summary>
		/// Notification with a action and argument.
		/// </summary>
		class NotifyWithArg : INotify
		{
			public Action<GameObject, object> action;
			public object arg;
		}
		
		/// <summary>
		/// The list of functions that will be called.
		/// </summary>
		IList<INotify> m_actions = new List<INotify>();
		
		/// <summary>
		/// Call to execute actions.
		/// </summary>
		protected void OnEvent()
		{
			
			if(Disable) return;
			
			int count = m_actions.Count;
			for(int i = 0; i < count; i++)
			{
				INotify  notify = m_actions[i];
				
				if (notify is Notify)
				{
					Notify n = notify as Notify;
					n.action(gameObject);
				}
				else if (notify is NotifyWithArg)
				{
					NotifyWithArg n = notify as NotifyWithArg;
					n.action(gameObject, n.arg);
				}
			}
		}
		
		/// <summary>
		/// Add a action with a argument.
		/// </summary>
		public void AddAction(Action<GameObject, object> action, object arg)
		{
			NotifyWithArg notify = new NotifyWithArg();
			notify.action = action;
			notify.arg = arg;
			
			m_actions.Add(notify);
		}
		
		/// <summary>
		/// Add a action with no argument.
		/// </summary>
		public void AddAction(Action<GameObject> action)
		{
			Notify notify = new Notify();
			notify.action = action;
			
			m_actions.Add(notify);
		}
		
		
	}
}
