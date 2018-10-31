using System.Collections.Generic;

namespace Ceto.Common.Containers.Queues
{
    ///<summary>
    /// Custom container that allows items to be looked up with a key like a
    /// dictionary while also retaining what order the items where added 
    /// to the container like a queue all with O(1) complexity. 
    /// Implemented by using a dictionary to store linked list nodes 
    /// and a linked list to store dictionary keyValuePairs.
    /// 
    /// Modification of the DictionaryQueue where the value is also the key.
    /// 
    ///</summary>
	public class SetQueue<VALUE> 
	{
		
		Dictionary<VALUE, LinkedListNode<VALUE>> m_dictionary;
		LinkedList<VALUE> m_list;

        ///<summary>
        /// Default constructor
        ///</summary>
		public SetQueue() 
		{
			m_dictionary = new Dictionary<VALUE, LinkedListNode<VALUE>>();
			m_list = new LinkedList<VALUE>();
		}

        ///<summary>
        /// Constructor that allows a comparer to define sorting of values
        ///</summary>
		public SetQueue(IEqualityComparer<VALUE> comparer) 
		{
			m_dictionary = new Dictionary<VALUE, LinkedListNode<VALUE>>(comparer);
			m_list = new LinkedList<VALUE>();
		}

        ///<summary>
        /// Subscript operator to access container like a array.
        /// Getter will return value at key. 
        /// Setter will replace value at key with new value.
        /// Will throw exception if key does not exist.
        ///</summary>
		public VALUE this[VALUE key]
		{
			get {
				return m_dictionary[key].Value;
			}
			set {
                Replace(key, value);
			}
		}

        ///<summary>
        /// Returns enumerator to allow container to be
        /// iterated in a foreach loop. Will Iterate values
        /// in the order they where added to the container
        /// from first to last.
        ///</summary>
		public IEnumerator<VALUE> GetEnumerator()
		{


            var e = m_list.GetEnumerator();
            while (e.MoveNext())
            {
                yield return e.Current;
            }

		}

        ///<summary>
        /// Returns true if key present in container
        ///</summary>
		public bool Contains(VALUE val) {
			return m_dictionary.ContainsKey(val);
		}

        ///<summary>
        /// Replace value at key with new value.
        /// Will throw exception if key does not exist
        ///</summary>
        public void Replace(VALUE key, VALUE val)
        {
            LinkedListNode<VALUE> node = m_dictionary[key];
            node.Value = val;
            m_dictionary.Remove(key);
            m_dictionary.Add(val, node);
        }

        ///<summary>
        /// Add key with value to start of container
        ///</summary>
		public void AddFirst(VALUE val) {
			m_dictionary.Add(val, m_list.AddFirst(val));
		}

        ///<summary>
        /// Add key with value to end of container
        ///</summary>
		public void AddLast(VALUE val) {
			m_dictionary.Add(val, m_list.AddLast(val));
		}

        ///<summary>
        /// How many values in container
        ///</summary>
		public int Count {
			get { return m_dictionary.Count; }
		}

        ///<summary> 
        /// True if there are no values in container
        ///</summary>
		public bool IsEmpty {
			get { return (m_dictionary.Count == 0); }
		}

        ///<summary>
        /// Returns the first value in container.
        ///</summary>
		public VALUE First() {
			return m_list.First.Value;
		}

        ///<summary>
        /// Returns the last value in container.
        ///</summary>
		public VALUE Last() {
			return m_list.Last.Value;
		}

        ///<summary>
        /// Removes the first value in the container and returns it.
        ///</summary>
		public VALUE RemoveFirst()
		{
			LinkedListNode<VALUE> node = m_list.First;
			m_list.RemoveFirst();
			m_dictionary.Remove(node.Value);
			return node.Value;
		}

        ///<summary>
        /// Removes the last value in the container and returns it.
        ///</summary>
		public VALUE RemoveLast()
		{
			LinkedListNode<VALUE> node = m_list.Last;
			m_list.RemoveLast();
			m_dictionary.Remove(node.Value);
			return node.Value;
		}

        ///<summary>
        /// Remove value from container.
        ///</summary>
		public void Remove(VALUE val)
		{
			LinkedListNode<VALUE> node = m_dictionary[val];
			m_dictionary.Remove(val);
			m_list.Remove(node);
		}

        ///<summary>
        /// Clear all values from container.
        ///</summary>
		public void Clear() 
		{
			m_dictionary.Clear();
			m_list.Clear();
		}
			         
	}

}
