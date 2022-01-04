﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Cathei.BakingSheet
{
    public abstract class SheetRow<TKey> : ISheetRow
    {
        public TKey Id { get; set; }

        object ISheetRow.Id => Id;

        public virtual void PostLoad(SheetConvertingContext context)
        {
            using (context.Logger.BeginScope(Id))
            {
                SheetUtility.MapReferences(context, this);
            }
        }

        public virtual void VerifyAssets(SheetConvertingContext context)
        {
            using (context.Logger.BeginScope(Id))
            {
                SheetUtility.VerifyAssets(context, this);
            }
        }
    }

    public abstract class SheetRowElem : ISheetRowElem
    {
        [NonSerialized]
        public int Index { get; internal set; }

        public virtual void PostLoad(SheetConvertingContext context)
        {
            using (context.Logger.BeginScope(Index))
            {
                SheetUtility.MapReferences(context, this);
            }
        }

        public virtual void VerifyAssets(SheetConvertingContext context)
        {
            using (context.Logger.BeginScope(Index))
            {
                SheetUtility.VerifyAssets(context, this);
            }
        }
    }

    public abstract class SheetRowArray<TKey, TElem> : SheetRow<TKey>, IEnumerable<TElem>, ISheetRowArray
        where TElem : SheetRowElem, new()
    {
        // setter is necessary for reflection
        public List<TElem> Arr { get; private set; } = new List<TElem>();

        IList ISheetRowArray.Arr => Arr;
        public Type ElemType => typeof(TElem);

        public int Count => Arr.Count;
        public TElem this[int idx] => Arr[idx];

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<TElem> GetEnumerator() => Arr.GetEnumerator();

        public override void PostLoad(SheetConvertingContext context)
        {
            base.PostLoad(context);

            using (context.Logger.BeginScope(Id))
            {
                for (int idx = 0; idx < Arr.Count; ++idx)
                {
                    Arr[idx].Index = idx;
                    Arr[idx].PostLoad(context);
                }
            }
        }

        public override void VerifyAssets(SheetConvertingContext context)
        {
            base.VerifyAssets(context);

            using (context.Logger.BeginScope(Id))
            {
                for (int idx = 0; idx < Arr.Count; ++idx)
                {
                    Arr[idx].VerifyAssets(context);
                }
            }
        }
    }

    // Convenient shorthand
    public abstract class SheetRow : SheetRow<string> {}

    // Convenient shorthand
    public abstract class SheetRowArray<TElem> : SheetRowArray<string, TElem>
        where TElem : SheetRowElem, new() {}
}
