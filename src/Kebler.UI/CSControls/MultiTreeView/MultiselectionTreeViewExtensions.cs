﻿using System.Collections.Generic;
using Kebler.Models.Tree;
using Kebler.UI.CSControls.MuliTreeView;

namespace Kebler.UI.CSControls.MultiTreeView
{
    public static class MultiselectionTreeViewExtensions
    {
        public static ExpandedTreeViewElement GetExpandedItems(
          this MultiselectionTreeView treeView)
        {
            MultiselectionTreeViewItem rootItem = treeView.RootItem;
            return rootItem != null ? GetExpandedItems(rootItem) : null;
        }

        public static void CollapseAllChildren(this MultiselectionTreeViewItem item)
        {
            foreach (MultiselectionTreeViewItem child in item.Children)
            {
                child.CollapseAllChildren();
                if (child.HasChildren)
                    child.IsExpanded = false;
            }
        }

        public static void ExpandAllChildren(this MultiselectionTreeViewItem item)
        {
            foreach (MultiselectionTreeViewItem child in item.Children)
            {
                child.ExpandAllChildren();
                if (child.HasChildren)
                    child.IsExpanded = true;
            }
        }

        public static void SetExpandedItems(
          this MultiselectionTreeView treeView,
          ExpandedTreeViewElement root)
        {
            if (root == null)
                return;
            foreach (ExpandedTreeViewElement child in root.Children)
                SetExpandedItems(treeView.RootItem, child);
        }

        private static void SetExpandedItems(
          MultiselectionTreeViewItem treeViewItem,
          ExpandedTreeViewElement childToExpand)
        {
            foreach (MultiselectionTreeViewItem child1 in treeViewItem.Children)
            {
                if (child1.Title == childToExpand.Name)
                {
                    child1.IsExpanded = true;
                    foreach (ExpandedTreeViewElement child2 in childToExpand.Children)
                        SetExpandedItems(child1, child2);
                    break;
                }
            }
        }

        private static ExpandedTreeViewElement GetExpandedItems(
          MultiselectionTreeViewItem item)
        {
            List<ExpandedTreeViewElement> expandedTreeViewElementList = new List<ExpandedTreeViewElement>();
            foreach (MultiselectionTreeViewItem child in item.Children)
            {
                if (child.IsExpanded)
                    expandedTreeViewElementList.Add(GetExpandedItems(child));
            }
            return new ExpandedTreeViewElement(item.Title, expandedTreeViewElementList.ToArray());
        }
    }
}
