using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace NodeMCU_Studio_2015
{
    public class InterTextBox : TextBox
    {
        private string cmd = "";
        private bool enter_down = false;

        protected override void OnPreviewKeyDown( KeyEventArgs e ) {
            if ( e.Key == Key.Enter ) {
                enter_down = true;
                int x = 0;
            } else if ( e.Key == Key.Back ) {
                if ( cmd.Length == 0 ) {
                    e.Handled = true;
                }
            } else if ( e.Key == Key.Delete ) {
                if ( cmd.Length == 0 ) {
                    e.Handled = true;
                }
            } else if ( e.Key == Key.Left ) {
                e.Handled = true;
            } else if ( e.Key == Key.Right ) {
                e.Handled = true;
            } else if ( e.Key == Key.Up ) {
                e.Handled = true;
            } else if ( e.Key == Key.Space ) {
                var text = Workspace.Instance.Write( " " , false );
                cmd += " ";
            }
            base.OnPreviewKeyDown( e );
        }

        protected override void OnKeyUp( KeyEventArgs e ) {
            if ( e.Key == Key.Enter ) {
                if ( enter_down ) {
                    enter_down = false;
                    e.Handled = true;
                    Workspace.Instance.WriteLine();
                    cmd = "";
                }
                base.OnKeyUp( e );
            } else if ( e.Key == Key.Back ) {
                byte back = ( byte ) 0x08;
                if ( cmd.Length == 0 ) {
                    e.Handled = true;
                } else {
                    Workspace.Instance.Write( back , 3 );
                    cmd = cmd.Remove( cmd.Length - 1 );
                }
                base.OnKeyUp( e );
            } else if ( e.Key == Key.Delete ) {
                byte del = ( byte ) 0x7f;
                if ( cmd.Length == 0 ) {
                    e.Handled = true;
                } else {
                    //Workspace.Instance.Write( del , 3 );
                }
                base.OnKeyUp( e );
            } else {
                base.OnKeyUp( e );
            }
        }

        protected override void OnTextInput( TextCompositionEventArgs e ) {
            var text = Workspace.Instance.Write( e.Text , false );
            cmd += e.Text;
            base.OnTextInput( e );
        }
    }
}
