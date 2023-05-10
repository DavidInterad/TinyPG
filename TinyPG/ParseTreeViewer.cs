// Copyright 2008 - 2010 Herre Kuijpers - <herre.kuijpers@gmail.com>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

using System.Drawing;
using System.Windows.Forms;
using TinyPG.Debug;

namespace TinyPG
{
    /// <summary>
    /// this class helps populate the tree view given a parse tree
    /// </summary>
    public static class ParseTreeViewer
    {
        public static void Populate(TreeView treeView, IParseTree parseTree)
        {
            treeView.Visible = false;
            treeView.SuspendLayout();
            treeView.Nodes.Clear();
            treeView.Tag = parseTree;

            var start = parseTree.INodes[0];
            var node = new TreeNode(start.Text)
            {
                Tag = start,
                ForeColor = Color.SteelBlue,
            };
            treeView.Nodes.Add(node);

            PopulateNode(node, start);
            treeView.ExpandAll();
            treeView.ResumeLayout();
            treeView.Visible = true;
        }

        private static void PopulateNode(TreeNode node, IParseNode start)
        {
            foreach (var ipn in start.INodes)
            {
                var tn = new TreeNode(ipn.Text)
                {
                    Tag = ipn,
                };
                node.Nodes.Add(tn);
                PopulateNode(tn, ipn);
            }
        }
    }
}
