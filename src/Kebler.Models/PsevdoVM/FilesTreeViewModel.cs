﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using Kebler.Models.Torrent;
using Kebler.Models.Tree;

namespace Kebler.Models.PsevdoVM
{
    public class FilesTreeViewModel : INotifyPropertyChanged
    {
        public MultiselectionTreeViewItem Files { get; set; } = new MultiselectionTreeViewItem();
        public event PropertyChangedEventHandler PropertyChanged;

        public Thickness Border { get; set; } = new Thickness(0);



        public uint[] getFilesWantedStatus(bool status)
        {
            uint[] output = new uint[0];
            foreach (var tm in Files.Children)
            {
                var ids = get(tm, status);
                merge(ref output, ref ids, out output);
            }
            return output;
        }

        private void merge(ref uint[] array1, ref uint[] array2, out uint[] result)
        {
            int array1OriginalLength = array1.Length;
            Array.Resize(ref array1, array1OriginalLength + array2.Length);
            Array.Copy(array2, 0, array1, array1OriginalLength, array2.Length);
            result = array1;
        }

        private uint[] get(MultiselectionTreeViewItem Node, bool search)
        {
            uint[] output = new uint[0];
            if (Node.HasChildren)
            {
                foreach (var item in Node.Children)
                {
                    if (item.HasChildren)
                    {
                        var ids = get(item, search);
                        merge(ref output, ref ids, out output);
                    }
                    else
                    {
                        if (item.IsChecked == search)
                        {
                            Array.Resize(ref output, output.Length + 1);
                            output[output.GetUpperBound(0)] = item.IndexPattern;
                            //dd.Add(item.IndexPattern);
                        }
                    }
                }
            }
            else
            {
                if (Node.IsChecked == search)
                {
                    Array.Resize(ref output, output.Length + 1);
                    output[output.GetUpperBound(0)] = Node.IndexPattern;
                    //dd.Add(Node.IndexPattern);
                }
            }

            return output;
        }


        public void UpdateFilesTree(ref TorrentInfo torrent)
        {

            var items = createTree(ref torrent);

            var newItems = new MultiselectionTreeViewItem { IsExpanded = true };
            newItems.Children.Add(items);

            Files = newItems;
        }

        private static MultiselectionTreeViewItem createTree(ref TorrentInfo torrent)
        {

            var root = new MultiselectionTreeViewItem { Title = torrent.Name };
            foreach (var itm in torrent.Files)
            {
                itm.Name = itm.Name.Replace(torrent.Name, string.Empty).TrimStart('/', '\\');
            }



            var count = 0U;
            for (uint i = 0; i < torrent.Files.Length; i++)
            {
                if (torrent.FileStats != null)
                    createNodes(ref root, torrent.Files[i].Name, count, torrent.FileStats[i].Wanted);
                createNodes(ref root, torrent.Files[i].Name, count, true);

                count++;
            }

            return root;
        }


        private static void createNodes(ref MultiselectionTreeViewItem root, string file, uint index, bool wanted)
        {
           
                file = file.Replace('/', '\\');
                var dirs = file.Split('\\');

                var last = root;
                foreach (var s in dirs)
                {
                    if (string.IsNullOrEmpty(s))
                        continue;

                    if (last.Children.All(x => x.Title != s))
                    {
                        //if not found children
                        var pth = new MultiselectionTreeViewItem { Title = s, IsExpanded = true };
                        last.Children.Add(pth);
                        last = pth;
                    }
                    else
                    {
                        last = last.Children.First(p => p.Title == s);
                    }
                }
                last.IndexPattern = index;
                last.IsChecked = wanted;
       
        }



        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
