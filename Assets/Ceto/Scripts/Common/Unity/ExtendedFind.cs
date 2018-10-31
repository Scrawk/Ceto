using UnityEngine;
using System.Collections.Generic;

namespace Ceto.Common.Unity.Utility
{

	/// <summary>
	/// Extended find methods for getting components and interfaces from game objects.
	/// </summary>
	static public class ExtendedFind
	{

		/// <summary>
		/// Get the first interface found attached to the gameobject
		/// of type T and return it. Return null if none found.
		/// </summary>
		static public T GetInterface<T>(GameObject obj) where T : class
		{
			Component[] components = obj.GetComponents<Component>();
			
			foreach(Component c in components) {
				if(c is T) return c as T;
			}
			
			return null;
		}

        /// <summary>
        /// Get the first interface found attached to a child of the gameobject
        /// of type T and return it. Return null if none found.
        /// </summary>
		static public T GetInterfaceInChildren<T>(GameObject obj) where T : class
		{
			Component[] components = obj.GetComponentsInChildren<Component>();
			
			foreach(Component c in components) {
				if(c is T) return c as T;
			}
			
			return null;
		}

        /// <summary>
        /// Get the first interface found attached to a immediate child of gameobject
        /// of type T and return it. Return null if none found.
        /// </summary>
		static public T GetInterfaceImmediateChildren<T>(GameObject obj) where T : class
		{
			foreach(Transform child in obj.transform)
			{
				Component[] components = child.GetComponents<Component>();
				
				foreach(Component c in components) {
					if(c is T) return c as T;
				}
			}

			return null;
		}
		
        /// <summary>
        /// Get all interfaces attached to gameobject of type T and return them.
        /// </summary>
		static public T[] GetInterfaces<T>(GameObject obj) where T : class
		{
			Component[] components = obj.GetComponents<Component>();

			List<T> list = new List<T>();
			
			foreach(Component c in components) {
				if(c is T) list.Add(c as T);
			}
			
			return list.ToArray();
		}

        /// <summary>
        /// Get all the interfaces attached to any of the children of gameobject
        /// of type T and return them.
        /// </summary>
		static public T[] GetInterfacesInChildren<T>(GameObject obj) where T : class
		{
			Component[] components = obj.GetComponentsInChildren<Component>();
			
			List<T> list = new List<T>();
			
			foreach(Component c in components) {
				if(c is T) list.Add(c as T);
			}
			
			return list.ToArray();
		}

        /// <summary>
        /// Get all the interfaces attached to any of the immediate children of 
        /// gameobject of type T and return them.
        /// </summary>
		static public T[] GetInterfacesImmediateChildren<T>(GameObject obj) where T : class
		{
			List<T> list = new List<T>();

			foreach(Transform child in obj.transform)
			{
				Component[] components = child.GetComponents<Component>();
				
				foreach(Component c in components) {
					if(c is T) list.Add(c as T);
				}
			}

			return list.ToArray();
		}

        /// <summary>
        /// Get the first component found in the immediate parent of gameobject
        /// of type T. Return null if none found.
        /// </summary>
		static public T GetComponetInImmediateParent<T>(GameObject obj) where T : Component
		{
			if(obj.transform.parent == null) return null;

			return obj.transform.parent.GetComponent<T>();
		}

        /// <summary>
        /// Get all the components found in the immediate parent of gameobject of type T.
        /// </summary>
		static public T[] GetComponentsInImmediateParent<T>(GameObject obj) where T : Component
		{
            if (obj.transform.parent == null) return new T[0];

			return obj.transform.parent.GetComponents<T>();
		}

        /// <summary>
        /// Get the first component found in the immediate children of gameobject
        /// of type T. Return null if none found.
        /// </summary>
		static public T GetComponetInImmediateChildren<T>(GameObject obj) where T : Component
		{
			foreach(Transform child in obj.transform)
			{
				T component = child.GetComponent<T>();
				
				if(component != null) return component;
			}
			
			return null;
		}

        /// <summary>
        /// Get all the components found in the immediate children of gameobject of type T.
        /// </summary>
		static public T[] GetComponetsInImmediateChildren<T>(GameObject obj) where T : Component
		{
			List<T> list = new List<T>();

			foreach(Transform child in obj.transform)
			{
				T[] components = child.GetComponents<T>();
				
				foreach(T c in components) {
					list.Add(c);
				}
			}
			
			return list.ToArray();
		}

        /// <summary>
        /// Returns the a component of type T on a named game object.
        /// Returns null if no component found or no game object 
        /// called name exists.
        /// </summary>
		static public T FindComponentOnGameObject<T>(string name) where T : Component
		{
			GameObject go = GameObject.Find(name);

			if(go == null) return null;

			return go.GetComponent<T>();
		}

        /// <summary>
        /// Returns the all components of type T on a named game object.
        /// Returns empty array if no component found or no game object 
        /// called name exists.
        /// </summary>
		static public T[] FindComponentsOnGameObject<T>(string name) where T : Component
		{
			GameObject go = GameObject.Find(name);
			
			if(go == null) return new T[0];
			
			return go.GetComponents<T>();
		}

        /// <summary>
        /// Returns the a interface of type T on a named game object.
        /// Returns null if no interface found or no game object 
        /// called name exists.
        /// </summary>
		static public T FindInterfaceOnGameObject<T>(string name) where T : class
		{
			GameObject go = GameObject.Find(name);
			
			if(go == null) return null;
			
			return GetInterface<T>(go);
		}

        /// <summary>
        /// Returns the all interfaces of type T on a named game object.
        /// Returns empty array if no interfaces found or no game object 
        /// called name exists.
        /// </summary>
		static public T[] FindInterfacesOnGameObject<T>(string name) where T : class
		{
			GameObject go = GameObject.Find(name);
			
			if(go == null) return new T[0];
			
			return GetInterfaces<T>(go);
		}
		
	}

}












