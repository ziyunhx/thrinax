using System;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	internal class Cache
	{
		private sealed class head_t
		{
			private Cache _enclosingInstance;

			internal head_t prev;

			internal head_t next;

			internal float[] data;

			internal int len;

			public Cache EnclosingInstance
			{
				get
				{
					return this._enclosingInstance;
				}
			}

			public head_t(Cache enclosingInstance)
			{
				this._enclosingInstance = enclosingInstance;
			}
		}

		private int _count;

		private long _size;

		private head_t[] head;

		private head_t lru_head;

		public Cache(int count, long size)
		{
			this._count = count;
			this._size = size;
			this.head = new head_t[this._count];
			for (int i = 0; i < this._count; i++)
			{
				this.head[i] = new head_t(this);
			}
			this._size /= 4L;
			this._size -= this._count * 4;
			this.lru_head = new head_t(this);
			this.lru_head.next = (this.lru_head.prev = this.lru_head);
		}

		private void lru_delete(head_t h)
		{
			h.prev.next = h.next;
			h.next.prev = h.prev;
		}

		private void lru_insert(head_t h)
		{
			h.next = this.lru_head;
			h.prev = this.lru_head.prev;
			h.prev.next = h;
			h.next.prev = h;
		}

		private static void swap<T>(ref T lhs, ref T rhs)
		{
			T val = lhs;
			lhs = rhs;
			rhs = val;
		}

		public int GetData(int index, ref float[] data, int len)
		{
			head_t head_t = this.head[index];
			if (head_t.len > 0)
			{
				this.lru_delete(head_t);
			}
			int num = len - head_t.len;
			if (num > 0)
			{
				while (this._size < num)
				{
					head_t next = this.lru_head.next;
					this.lru_delete(next);
					this._size += next.len;
					next.data = null;
					next.len = 0;
				}
				float[] array = new float[len];
				if (head_t.data != null)
				{
					Array.Copy(head_t.data, 0, array, 0, head_t.len);
				}
				head_t.data = array;
				this._size -= num;
				Cache.swap<int>(ref head_t.len, ref len);
			}
			this.lru_insert(head_t);
			data = head_t.data;
			return len;
		}

		public void SwapIndex(int i, int j)
		{
			if (i != j)
			{
				if (this.head[i].len > 0)
				{
					this.lru_delete(this.head[i]);
				}
				if (this.head[j].len > 0)
				{
					this.lru_delete(this.head[j]);
				}
				Cache.swap<float[]>(ref this.head[i].data, ref this.head[j].data);
				Cache.swap<int>(ref this.head[i].len, ref this.head[j].len);
				if (this.head[i].len > 0)
				{
					this.lru_insert(this.head[i]);
				}
				if (this.head[j].len > 0)
				{
					this.lru_insert(this.head[j]);
				}
				if (i > j)
				{
					Cache.swap(ref i, ref j);
				}
				for (head_t next = this.lru_head.next; next != this.lru_head; next = next.next)
				{
					if (next.len > i)
					{
						if (next.len > j)
						{
							Cache.swap<float>(ref next.data[i], ref next.data[j]);
						}
						else
						{
							this.lru_delete(next);
							this._size += next.len;
							next.data = null;
							next.len = 0;
						}
					}
				}
			}
		}
	}
}
