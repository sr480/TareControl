using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace ChartControl
{
    public class ValueMemberDefinition : DependencyObject
    {
        public string Member
        {
            get { return (string)GetValue(MemberProperty); }
            set { SetValue(MemberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Member.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MemberProperty =
            DependencyProperty.Register("Member", typeof(string), typeof(ValueMemberDefinition));



        public Brush Color
        {
            get { return (Brush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Brush), typeof(ValueMemberDefinition), new UIPropertyMetadata(Brushes.DarkRed));



    }
}
