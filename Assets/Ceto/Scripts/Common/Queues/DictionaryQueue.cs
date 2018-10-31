
using System.Collections;
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
    ///</summary>
	public class DictionaryQueue<KEY, VALUE>
	{
		
		Dictionary<KEY, LinkedListNode<KeyValuePair<KEY,VALUE>>> m_dictionary;
		LinkedList<KeyValuePair<KEY,VALUE>> m_list;

        ///<summary>
        /// Default constructor
        ///</summary>
		public DictionaryQueue() 
		{
			m_dictionary = new Dictionary<KEY, LinkedListNode<KeyValuePair<KEY,VALUE>>>();
			m_list = new LinkedList<KeyValuePair<KEY,VALUE>>();
		}

        ///<summary>
        /// Constructor that allows a comparer to define equality of values
        ///</summary>
		public DictionaryQueue(IEqualityComparer<KEY> comparer) 
		{
			m_dictionary = new Dictionary<KEY, LinkedListNode<KeyValuePair<KEY,VALUE>>>(comparer);
			m_list = new LinkedList<KeyValuePair<KEY,VALUE>>();
		}

        ///<summary>
        /// Subscript operator to access container like a array.
        /// Getter will return value at key. 
        /// Setter will replace value at key with new value.
        /// Will throw exception if key does not exist.
        ///</summary>
        public VALUE this[KEY key]
		{
			get {
				return m_dictionary[key].Value.Value;
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
            while(e.MoveNext())
            {
                yield return e.Current.Value;
            }

		}

        ///<summary>
        /// Returns true if key present in container
        ///</summary>
		public bool ContainsKey(KEY key) {
			return m_dictionary.ContainsKey(key);
		}

        ///<summary>
        /// Replace value at key with new value.
        /// Will throw exception if key does not exist
        ///</summary>
		public void Replace(KEY key, VALUE val) {
			LinkedListNode<KeyValuePair<KEY,VALUE>> node = m_dictionary[key];
			node.Value = new KeyValuePair<KEY, VALUE>(key, val);
		}

        ///<summary>
        /// Add key with value to start of container
        ///</summary>
		public void AddFirst(KEY key, VALUE val) {
			m_dictionary.Add(key, m_list.AddFirst(new KeyValuePair<KEY,VALUE>(key,val)));
		}

        ///<summary>
        /// Add key with value to end of container
        ///</summary>
		public void AddLast(KEY key, VALUE val) {
			m_dictionary.Add(key, m_list.AddLast(new KeyValuePair<KEY,VALUE>(key,val)));
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
        public bool IsEmpty
        {
            get { return (m_dictionary.Count == 0); }
        }

        ///<summary>
        /// Returns the first value in container.
        ///</summary>
        public KeyValuePair<KEY,VALUE> First() {
			return m_list.First.Value;
		}

        ///<summary>
        /// Returns the last value in container.
        ///</summary>
        public KeyValuePair<KEY,VALUE> Last() {
			return m_list.Last.Value;
		}

        ///<summary>
        /// Removes the first value in the container and returns it.
        ///</summary>
        public VALUE RemoveFirst()
		{
			LinkedListNode<KeyValuePair<KEY,VALUE>> node = m_list.First;
			m_list.RemoveFirst();
			m_dictionary.Remove(node.Value.Key);
			return node.Value.Value;
		}

        ///<summary>
        /// Removes the last value in the container and returns it.
        ///</summary>
        public VALUE RemoveLast()
		{
			LinkedListNode<KeyValuePair<KEY,VALUE>> node = m_list.Last;
			m_list.RemoveLast();
			m_dictionary.Remove(node.Value.Key);
			return node.Value.Value;
		}

        ///<summary>
        /// Remove value from container at key and return it.
        ///</summary>
		public VALUE Remove(KEY key)
		{
			LinkedListNode<KeyValuePair<KEY,VALUE>> node = m_dictionary[key];
			m_dictionary.Remove(key);
			m_list.Remove(node);
			return node.Value.Value;
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



























