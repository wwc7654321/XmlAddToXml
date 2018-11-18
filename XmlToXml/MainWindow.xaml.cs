using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace XmlToXml
{
    using ElemIndex = Tuple<string, string, int>;
    internal class OneXmlNode : INotifyPropertyChanged
    {
        public const string COMMENT_NAME = "<!--Comment-->";

        public OneXmlNode ParentNode { get; set; }
        
        public ElemIndex ChildIndex { get; set; }
        public string Icon { get; set; }
        public bool IsChecked { get; set; }
        public bool IsEnable { get; set; } = true;
        public string Path { get; set; }
        public string NodeName { get; set; }
        public int Type { get; set; }

        public string Text { get; set; }
        public string ReplacedText { get; set; }
        public string ShowText { get {
                string txt;
                if (!string.IsNullOrEmpty(ReplacedText)) { txt = ReplacedText; } else { txt = Text; }
                if (Type == 2) { return "<!--" + txt + "-->"; } return txt;
            } }
        public object Tag { get; set; } // b的tag 指向a, a的Tag只有从b新导入的指向b

        _V TryGet<_K,_V>(Dictionary<_K,_V> dic, _K key, _V def)
        {
            if(dic.ContainsKey(key))
            {
                return dic[key];
            }
            return def;
        }

        protected BindingList<OneXmlNode> child;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String name = "")
        { 
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public void OnAllChanged()
        {
            foreach(var a in typeof(OneXmlNode).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                OnPropertyChanged(a.Name);
            }
        }

        public BindingList<OneXmlNode> Children {
            get
            {
                Dictionary<string, int> elem_dic = new Dictionary<string, int>();
                if (Root == null)
                {
                    return new BindingList<OneXmlNode>();
                }
                if (child.Any())
                {
                    return child;
                }
                ElemIndex last_elem = new ElemIndex("", "", 0);
                int j = 0;
                foreach(var n in Root.Nodes())
                {
                    if (n.NodeType == XmlNodeType.Comment)
                    {
                        string key = COMMENT_NAME;
                        /*var val = TryGet(elem_dic, key, 0);
                        elem_dic[key] = val +1;*/

                        OneXmlNode ch = new OneXmlNode(((XComment)n), new ElemIndex(key, last_elem.Item1+"_"+ last_elem.Item3, j), this);
                        child.Add(ch);
                        j++;
                    }
                    else if (n.NodeType == XmlNodeType.Element)
                    {
                        XElement e = (XElement) n;
                        string key = e.Name.ToString();
                        var val = TryGet(elem_dic, key, 0);
                        elem_dic[key] = val + 1;
                        OneXmlNode ch = new OneXmlNode(e, new ElemIndex(key,"", val), this);
                        child.Add(ch);
                        j = 0;
                        last_elem = ch.ChildIndex;
                    } 
                }

                return child;
            }
            protected set { child = value; }
        }


        public XNode GeneralNode { get { if (Root == null) { return Root; } else return XCommentNode; } }
        public XElement Root { get; set; }
        public XComment XCommentNode { get; set; }

        protected void SomeInit(OneXmlNode parent, string node_name)
        {
            child = new BindingList<OneXmlNode>();
            //elem_dic=new Dictionary<string, int>();
            NodeName = node_name;


            if (parent != null)
            { 
                IsEnable = parent.IsEnable;
                IsChecked = parent.IsChecked;
                Path = parent.Path + '/' + NodeName;
            }
            else
            {
                Path= '/' + NodeName;
            }

            ParentNode = parent;
        }

        public OneXmlNode(XElement root, ElemIndex pos = null, OneXmlNode parent=null)
        {
            SomeInit(parent, root.Name.ToString());

            Root = root; 
            Text = Root.Name.ToString();
            if (pos == null)
            {
                ChildIndex = new ElemIndex("","",0);
            }
            else
            {
                ChildIndex = pos;
            }
            Type = 1;
        }

        public OneXmlNode(XComment comment, ElemIndex pos , OneXmlNode parent)
        {
            SomeInit(parent, COMMENT_NAME);
            XCommentNode = comment;
            Text = comment.Value;

            ChildIndex = pos;
            Type = 2;
        }
    }


    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool b_changed = false;
        private string file1, file2;
        private string xml_str1, xml_str2;
        private OneXmlNode list1_nodes, list2_nodes;

        public MainWindow()
        {
            InitializeComponent();
            list2.ItemTemplate = list1.ItemTemplate;
        }
        private void Btn_f1_Click(object sender, RoutedEventArgs e)
        {
            FileDialog file = new OpenFileDialog();
            file.Filter = "*.xml|*.xml";
            if(!file.ShowDialog().GetValueOrDefault())
            {
                return;
            }
            f1.Text = file.FileName;
        }
        private void Btn_f2_Click(object sender, RoutedEventArgs e)
        {
            FileDialog file = new OpenFileDialog();
            file.Filter = "*.xml|*.xml";
            if (!file.ShowDialog().GetValueOrDefault())
            {
                return;
            }
            f2.Text = file.FileName;
        }

        private void F1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(f1.Text == file1)
            {
                return;
            }
            if(b_changed)
            {
                if(MessageBox.Show("Xml changed! continue will loss all unsaved xml! continue?", "Unsaved Result", MessageBoxButton.OKCancel)==MessageBoxResult.Cancel)
                {
                    f1.Text = file1;
                    return;
                }
            }
            file1 = f1.Text;
            ToDo1();
        }
        private void F2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (f2.Text == file2)
            {
                return;
            }
            file2 = f2.Text;
            ToDo2();
        }

        private void ToDo1()
        {
            var a = new SimpleXmlChecker(2);
            //list1.Items.Clear();
            b_changed = false;
            if (String.IsNullOrEmpty(file1) || !File.Exists(file1))
            {
                MessageBox.Show("No file found");
                return;
            }

            String xml = File.ReadAllText(file1);
            xml_str1 = xml;
            if (!a.Check(xml))
            {
                MessageBox.Show("\"" + file1 + "+\" " + a.err_desc + "!\n" + a.detailed_err);
                return;
            }
            
            OneXmlNode node = new OneXmlNode(a.doc.Root);
            node.IsChecked = true;
            node.IsEnable = false;
            list1.ItemsSource = new BindingList<OneXmlNode> {node};
            list1_nodes = node;
            b_changed = false;
            btn_f2.IsEnabled = true;
        }

        private void ToDo2()
        {
            var a = new SimpleXmlChecker(2);
            //list2.Items.Clear();
            if (String.IsNullOrEmpty(file2) || !File.Exists(file2))
            {
                MessageBox.Show("No file2 found");
                return;
            }
            String xml = File.ReadAllText(file2);
            xml_str2 = xml;
            if (!a.Check(xml))
            {
                MessageBox.Show("\"" + file2 + "+\" " + a.err_desc + "!\n" + a.detailed_err);
                return;
            }
            OneXmlNode node = new OneXmlNode(a.doc.Root); 
            list2.ItemsSource = new BindingList<OneXmlNode> { node };
            list2_nodes = node;
            Compare(list1_nodes, list2_nodes);
        }

        bool CompareOne(OneXmlNode a, OneXmlNode b)
        {
            if (a == null)
            {
                if (b != null)
                {
                    b.IsEnable = true;
                    return false;
                }
                return true;
            }
            else if(b==null){ return false; } 
            if (a.NodeName == b.NodeName)
            {
                if (a.Text == b.Text)   // possible? may be not
                {
                    if (a.Tag != b)
                    {
                        b.IsEnable = false;
                    }
                    return true;
                }
            }
            b.IsEnable = true;
            return false;
        }
        void Compare(OneXmlNode a, OneXmlNode b)
        {
            CompareOne(a, b);
            var map = new Dictionary<ElemIndex, OneXmlNode>();
            foreach (var ca in a.Children)
            {
                map[ca.ChildIndex] = ca;
            }
            foreach (var cb in b.Children)
            {
                if (map.ContainsKey(cb.ChildIndex))
                {
                    var ca = map[cb.ChildIndex];
                    cb.Tag = new OneXmlNode[2] { a, ca };
                    Compare(ca, cb);
                }
                else
                {
                    cb.Tag = new OneXmlNode[2] { a, null };
                    CompareOne(null, cb);
                }
            }
        }

        private void AllA_Checked(object sender, RoutedEventArgs e)
        {
            
            CheckAll(list1_nodes, true);
        }
        private void AllA_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckAll(list1_nodes, false);
        }
        private void AllB_Checked(object sender, RoutedEventArgs e)
        {
            CheckAll(list2_nodes, true);
        }
        private void AllB_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckAll(list2_nodes, false);
        }
        void CheckAll(OneXmlNode n, bool b)
        {
            var l = new List<OneXmlNode>();
            CheckAll_(n, l, b);
            foreach (var n1 in l)
            {
                n1.OnAllChanged();
            }
        }
        void CheckAll_(OneXmlNode n, List<OneXmlNode> changed, bool b)
        {
            if(n.IsEnable && n.IsChecked!=b)
            {
                n.IsChecked = b;
                changed.Add(n);
            }
            foreach(var nn in n.Children)
            {
                CheckAll_(nn, changed, b);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (b_changed)
            {
                if (MessageBox.Show("Xml changed! continue will loss all unsaved xml! continue?", "Unsaved Result", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }


        //bool TraverseAll(OneXmlNode a, OneXmlNode pa =null,XElement hint_pos = null)
        //{
        //    if (!(a.IsEnable && a.IsChecked))
        //    {
        //        return true;
        //    }
        //    if(a.Root==null)
        //    {
        //        if(a.Type==1)
        //        {
        //            a.Root = new XElement(a.NodeName);
        //        }
        //    }
        //    if (a.Root == null || a.Root.Document == null)
        //    {
        //        if (pa==null)
        //        {
        //            return false;
        //        }
        //        if(hint_pos!=null)
        //        { 

        //                pa.Root.Add() 
        //        }
        //    }
        //}
        private void btn_do_Click(object sender, RoutedEventArgs e)
        {

            //var map = new Dictionary<ElemIndex, KeyValuePair<int, OneXmlNode>>();
            //int i = 0;
            //foreach (var ca in other_parent.Children)
            //{
            //    map[ca.ChildIndex] = new KeyValuePair<int, OneXmlNode>(i++, ca);
            //}
            //i = 0;
            //OneXmlNode last_elem = null;
            //if (me.IsChecked.GetValueOrDefault(false))
            //{
            //    foreach (var cb in one.ParentNode.Children)
            //    {
            //        if (map.ContainsKey(cb.ChildIndex))
            //        {
            //            i = map[cb.ChildIndex].Key + 1;
            //            last_elem = map[cb.ChildIndex].Value;
            //        }
            //        if (cb == one)
            //        {
            //            break;
            //        }
            //    }
            //    // add // refresh // check b 
            //    OneXmlNode n;
            //    if (one.NodeName == OneXmlNode.COMMENT_NAME)
            //    {
            //        XComment nn = new XComment(one.Text);
            //        if (last_elem != null)
            //        {
            //            last_elem.Root.AddAfterSelf(nn);
            //        }
            //        else
            //        {
            //            other_parent.Root.Add(nn);
            //        }
            //        n = new OneXmlNode(one.Text, one.ChildIndex, other_parent);
            //    }
            //    else
            //    {
            //        XElement ne = new XElement(one.NodeName);
            //        if (last_elem != null)
            //        {
            //            last_elem.Root.AddAfterSelf(ne);
            //        }
            //        else
            //        {
            //            other_parent.Root.Add(ne);
            //        }
            //        n = new OneXmlNode(ne, one.ChildIndex, other_parent);
            //    }

            //}
            if (list1_nodes==null)return;
            string oldstr = xml_str1;
            string newstr = list1_nodes.Root.Document.ToString();


            int begnn = oldstr.IndexOf("\n\n", 0);
            int begkh = oldstr.IndexOf("<", 0);

            string result;
            if (!CombineXmlNL.CombineOldXml(out result, newstr, oldstr))
            {

                MessageBox.Show("err"+result);
            }

            File.WriteAllText(file1, result);

            MessageBox.Show("ok");
            b_changed = false;
        }
        

        private void ToggleButton_OnChecked_Changes_list1(object sender, RoutedEventArgs e)
        {
            CheckBox me = (CheckBox)sender;
            var one_a = (OneXmlNode)me.Tag;
            if (one_a.Tag != null)
            {
                var one_b = (OneXmlNode)null;
                try
                {
                    one_b = (OneXmlNode)one_a.Tag;
                }
                catch (System.InvalidCastException)
                {

                }
                if (one_b != null)
                {
                    one_b.IsChecked = false;
                    one_b.OnAllChanged();
                    one_a.ParentNode.Children.Remove(one_a);
                    if (one_a.Type == 1 && one_a.Root != null)
                    {
                        one_a.Root.Remove(); one_a.Root = null;
                    }
                    else if (one_a.Type == 2 && one_a.XCommentNode != null)
                    {
                        one_a.XCommentNode.Remove(); one_a.XCommentNode = null;
                    }
                }
                else
                {
                    if (!me.IsChecked.GetValueOrDefault(false))
                    {
                        one_b = ((Tuple<OneXmlNode>)one_a.Tag).Item1;
                        one_a.ReplacedText = "";
                        one_a.IsEnable = false;
                        one_a.Tag = null;

                        one_a.OnAllChanged();
                        one_a.IsChecked = true;
                        one_a.OnAllChanged();

                        one_b.IsChecked = false;
                        one_b.OnAllChanged();
                    }
                    if (one_a.Type == 2)
                    {
                        one_a.XCommentNode.Value = !string.IsNullOrEmpty(one_a.ReplacedText) ? one_a.ReplacedText : one_a.Text;
                    }
                }
            }
        }

        private void ToggleButton_OnChecked_Changes(object sender, RoutedEventArgs e)
        {
            CheckBox me = (CheckBox)sender; 
            var one = (OneXmlNode)me.Tag;
            if(one.Tag == null)
            {
                return;
            }
            var other_arr = (OneXmlNode[])null;
            try
            {
                other_arr = (OneXmlNode[])one.Tag;
            }
            catch(System.InvalidCastException)
            { 
                ToggleButton_OnChecked_Changes_list1(sender, e);    // 勾选A列的时候
                return;
            }
            b_changed = true;
            // 勾选B列的时候
            if (other_arr != null && other_arr.Count() == 2)
            {
                var other_parent = other_arr[0];
                if (other_parent == null)
                {
                    return;
                }
                var other = other_arr[1];
                if (other == null)
                {   // 新增节点
                    var map = new Dictionary<ElemIndex, KeyValuePair<int,OneXmlNode>>();
                    int i = 0;
                    foreach (var ca in other_parent.Children)       // 对A父节点下的子节点建立索引
                    {
                        map[ca.ChildIndex] = new KeyValuePair<int, OneXmlNode>(i++, ca);
                    }
                    i = 0;
                    OneXmlNode last_elem=null; 
                    if (me.IsChecked.GetValueOrDefault(false))      // 勾选
                    {
                        foreach (var cb in one.ParentNode.Children)
                        {
                            if (map.ContainsKey(cb.ChildIndex))
                            {
                                i = map[cb.ChildIndex].Key+1;
                                last_elem = map[cb.ChildIndex].Value;
                            }
                            if (cb == one)
                            {
                                break;
                            }
                        }
                        // add // refresh // check b 
                        OneXmlNode n;
                        if (one.NodeName == OneXmlNode.COMMENT_NAME)
                        {
                            var ee = new XComment(one.Text);
                            if (last_elem != null)
                            { 
                                
                                last_elem.GeneralNode.AddAfterSelf(ee);
                                //ee = (XComment)last_elem.GeneralNode.NextNode;
                            }
                            else
                            {
                                other_parent.Root.AddFirst(ee);
                                //ee = (XComment)other_parent.Root.FirstNode;
                            }
                            n = new OneXmlNode(ee, one.ChildIndex, other_parent);
                            
                        }
                        else
                        {
                            var ee = new XElement(one.Root.Name);
                            if (last_elem!=null)
                            {
                                last_elem.GeneralNode.AddAfterSelf(ee);
                                //ee = (XElement)last_elem.GeneralNode.NextNode;
                            }
                            else
                            {
                                other_parent.Root.AddFirst(ee);
                                //ee = (XElement)other_parent.Root.FirstNode;
                            }
                            n = new OneXmlNode(ee, one.ChildIndex, other_parent);
                        }
                        n.Tag = one;
                        n.IsEnable = true;
                        other_parent.Children.Insert(i, n); 
                        var tn = list1.ItemContainerGenerator.ContainerFromItem(other_parent) as TreeViewItem;
                        if (tn != null)
                        {
                            tn.BringIntoView();
                        }
                        Compare(n, one);
                    }
                    else                // 取消勾选
                    {
                        if (map.ContainsKey(one.ChildIndex))
                        {
                            other = other_parent.Children[map[one.ChildIndex].Key];
                            if (other.Type == 1 && other.Root != null)
                            {
                                other.Root.Remove(); other.Root = null;
                            }
                            else if (other.Type == 2 && other.XCommentNode != null)
                            {
                                other.XCommentNode.Remove(); other.XCommentNode = null;
                            }
                            other_parent.Children.RemoveAt(map[one.ChildIndex].Key);
                        }
                    }
                }
                else    // 修改节点
                {
                    if (me.IsChecked.GetValueOrDefault(false))
                    {
                        other.ReplacedText = one.Text;
                        other.IsChecked = true;
                        other.IsEnable = true;
                        other.Tag = new Tuple<OneXmlNode>(one);
                    }
                    else
                    {
                        other.ReplacedText = "";
                        other.IsEnable = false;
                        other.Tag = null;

                        other.OnAllChanged();
                        other.IsChecked = true;
                    }
                    if (other.Type == 2)
                    {
                        other.XCommentNode.Value = !string.IsNullOrEmpty(other.ReplacedText) ? other.ReplacedText : other.Text;
                    }
                    other.OnAllChanged();
                }
            }
        }
    }
}
