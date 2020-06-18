﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Kebler.Models.Tree
{
    [DebuggerDisplay("{Title}")]
    public class MultiselectionTreeViewItem : FlattenerNode, INotifyPropertyChanged
    {
        private bool _isExpanded = true;
        private bool _isHidden;
        private bool _isSelected;
        private bool _isChecked = true;

        private MultiselectionTreeViewItemCollection _children;

        public MultiselectionTreeViewItem ParentItem { get; private set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                    return;
                _isExpanded = value;
                if (_isExpanded)
                    OnExpanding();
                else
                    OnCollapsing();
                UpdateChildIsVisible(true);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }

        public virtual bool ShowExpander
        {
            get
            {
                return HasChildren;
            }
        }

        public void SetCheck(bool value)
        {
            this.IsChecked = value;

            if (!HasChildren) return;

            foreach (var child in _children)
                child.SetCheck(value);
        }

        public bool IsHidden
        {
            get => _isHidden;
            set
            {
                if (_isHidden == value)
                    return;
                _isHidden = value;
                if (ParentItem == null)
                    return;
                UpdateIsVisible(ParentItem.IsVisible && ParentItem.IsExpanded, true);
                ParentItem?.RaisePropertyChanged("ShowExpander");
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;
                _isSelected = value;
                var propertyChanged = PropertyChanged;
                propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value)
                    return;
                _isChecked = value;
                SetCheck(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }


        public bool HasChildren
        {
            get
            {
                return _children != null && _children.Count > 0;
            }
        }

        private void UpdateChildIsVisible(bool updateFlattener)
        {
            if (_children == null || _children.Count <= 0)
                return;
            bool parentIsVisibleAndExpanded = IsVisible && IsExpanded;
            foreach (MultiselectionTreeViewItem child in _children)
                child.UpdateIsVisible(parentIsVisibleAndExpanded, updateFlattener);
        }

        public int Level => ParentItem?.Level + 1 ?? 0;

        public virtual bool IsFocusable => true;

        public string Title { get; set; }

        public MultiselectionTreeViewItemCollection Children
        {
            get
            {
                if (_children == null)
                    _children = new MultiselectionTreeViewItemCollection(this);
                return _children;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void StartDrag(DependencyObject dragSource, MultiselectionTreeViewItem[] nodes)
        {
        }

        public virtual DragDropEffects GetDropEffect(DragEventArgs e, int index)
        {
            return DragDropEffects.None;
        }

        public void InternalDrop(DragEventArgs e, int index)
        {
            Drop(e, index);
        }

        public virtual void Drop(DragEventArgs e, int index)
        {
        }

        protected virtual IDataObject GetDataObject(MultiselectionTreeViewItem[] nodes)
        {
            return null;
        }

        public IEnumerable<MultiselectionTreeViewItem> Ancestors()
        {
            for (var node = this; node.ParentItem != null; node = node.ParentItem)
                yield return node.ParentItem;
        }

        public MultiselectionTreeViewItem Previous()
        {
            var treeFlattener = GetListRoot()._treeFlattener;
            if (treeFlattener != null)
            {
                var index = treeFlattener.IndexOf(this) - 1;
                if (index >= 0 && index < treeFlattener.Count)
                    return treeFlattener[index] as MultiselectionTreeViewItem;
            }
            return null;
        }

        public MultiselectionTreeViewItem Next()
        {
            Flattener treeFlattener = GetListRoot()._treeFlattener;
            if (treeFlattener != null)
            {
                int index = treeFlattener.IndexOf(this) + 1;
                if (index >= 0 && index < treeFlattener.Count)
                    return treeFlattener[index] as MultiselectionTreeViewItem;
            }
            return null;
        }

        public int FlatIndex()
        {
            var treeFlattener = GetListRoot()._treeFlattener;
            return treeFlattener?.IndexOf(this) ?? -1;
        }

        public void ApplyFilterToChild(MultiselectionTreeViewItem child, string filterString)
        {
            bool flag = child.MatchFilter(filterString);
            if (child.HasChildren)
            {
                child.EnsureChildrenFiltered(filterString);
                child.IsHidden = child.AreAllChildrenHidden();
            }
            else
                child.IsHidden = !flag;
        }

        internal void EnsureChildrenFiltered(string filterString)
        {
            foreach (MultiselectionTreeViewItem child in Children)
                ApplyFilterToChild(child, filterString);
        }

        private bool AreAllChildrenHidden()
        {
            if (_children == null)
                return true;
            for (int index = 0; index < _children.Count; ++index)
            {
                if (!_children[index].IsHidden)
                    return false;
            }
            return true;
        }

        protected virtual void OnExpanding()
        {
        }

        protected virtual void OnCollapsing()
        {
        }

        protected virtual bool MatchFilter(string filterString)
        {
            return string.IsNullOrEmpty(filterString) || Title.IndexOf(filterString, StringComparison.OrdinalIgnoreCase) != -1;
        }

        protected internal virtual void OnChildrenChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (MultiselectionTreeViewItem oldItem in e.OldItems)
                {
                    oldItem.ParentItem = null;
                    MultiselectionTreeViewItem multiselectionTreeViewItem = oldItem;
                    while (multiselectionTreeViewItem._children != null && multiselectionTreeViewItem._children.Count > 0)
                        multiselectionTreeViewItem = multiselectionTreeViewItem._children.Last<MultiselectionTreeViewItem>();
                    List<MultiselectionTreeViewItem> multiselectionTreeViewItemList = null;
                    int index = 0;
                    if (oldItem.IsVisible)
                    {
                        index = GetVisibleIndexForNode(oldItem);
                        multiselectionTreeViewItemList = oldItem.VisibleDescendantsAndSelf().ToList<MultiselectionTreeViewItem>();
                    }
                    RemoveNodes(oldItem, multiselectionTreeViewItem);
                    if (multiselectionTreeViewItemList != null)
                        GetListRoot()._treeFlattener?.NodesRemoved(index, multiselectionTreeViewItemList);
                }
            }
            if (e.NewItems != null)
            {
                MultiselectionTreeViewItem multiselectionTreeViewItem = e.NewStartingIndex == 0 ? null : _children[e.NewStartingIndex - 1];
                foreach (MultiselectionTreeViewItem newItem in e.NewItems)
                {
                    newItem.ParentItem = this;
                    newItem.UpdateIsVisible(IsVisible && IsExpanded, false);
                    for (; multiselectionTreeViewItem != null; multiselectionTreeViewItem = multiselectionTreeViewItem._children.Last<MultiselectionTreeViewItem>())
                    {
                        int? count = multiselectionTreeViewItem._children?.Count;
                        if (!(count.GetValueOrDefault() > 0 & count.HasValue))
                            break;
                    }
                    InsertNodeAfter(multiselectionTreeViewItem ?? this, newItem);
                    multiselectionTreeViewItem = newItem;
                    if (newItem.IsVisible)
                        GetListRoot()._treeFlattener?.NodesInserted(GetVisibleIndexForNode(newItem), newItem.VisibleDescendantsAndSelf());
                }
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ShowExpander"));
        }

        internal IEnumerable<MultiselectionTreeViewItem> VisibleDescendantsAndSelf()
        {
            return TreeTraversal.PreOrder(this, n => n.Children.Where(c => c.IsVisible));
        }

        private void UpdateIsVisible(bool parentIsVisibleAndExpanded, bool updateFlattener)
        {
            bool flag = parentIsVisibleAndExpanded && !IsHidden;
            if (IsVisible == flag)
                return;
            IsVisible = flag;
            InvalidateParents();
            List<MultiselectionTreeViewItem> multiselectionTreeViewItemList = null;
            if (updateFlattener && !flag)
                multiselectionTreeViewItemList = VisibleDescendantsAndSelf().ToList<MultiselectionTreeViewItem>();
            UpdateChildIsVisible(false);
            if (updateFlattener)
                CheckRootInvariants();
            if (multiselectionTreeViewItemList != null)
            {
                Flattener treeFlattener = GetListRoot()._treeFlattener;
                if (treeFlattener != null)
                {
                    treeFlattener.NodesRemoved(GetVisibleIndexForNode(this), multiselectionTreeViewItemList);
                    foreach (FlattenerNode flattenerNode in multiselectionTreeViewItemList)
                        flattenerNode.OnIsVisibleChanged();
                }
            }
            if (!(updateFlattener & flag))
                return;
            Flattener treeFlattener1 = GetListRoot()._treeFlattener;
            if (treeFlattener1 == null)
                return;
            treeFlattener1.NodesInserted(GetVisibleIndexForNode(this), VisibleDescendantsAndSelf());
            foreach (FlattenerNode flattenerNode in VisibleDescendantsAndSelf())
                flattenerNode.OnIsVisibleChanged();
        }
    }
}
