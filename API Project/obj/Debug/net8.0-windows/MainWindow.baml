   M S B A M L   `   `   `  ����F  BAPI Project, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)clr-namespace:API_ProjectAPI_Project  5   	       API_Project.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   +localclr-namespace:API_Project     5   	     � Title$  
MainWindow��5   	   $	��450��6   $	��800��6)   .��5	      � .+�5
   
   �� -   5         ��Name$ 	SearchBox��5
      $	��300��6%   $��25��61   $��10q�6=   $	��Top=�6I   $
��Left=�6a   �� 5   
   -   5        $��Suchen��5      $	��100��6"   $��25��6.   $��
320,10,0,0q�6:   $	��Top=�6N   $
��Left=�6f   6�   � 5   
   -   5         �Name$ 
LoadingBar��5      $��20��6*   $��
10,40,10,0q�66   $	��Top=�6J   $y�Hidden=�6b   &   System.Windows.Controls.DataGrid  5   
   -   5          Name$ ResultsDataGrid��5      $��10,70,10,10q�6,      AutoGenerateColumns$ False��6A      CanUserAddRows$ False��6]      Columns 5      0  *System.Windows.Controls.DataGridTextColumn  5      ,  &System.Windows.Controls.DataGridColumn   Header$ Bücher��6%      Width5  /System.Windows.Controls.DataGridLengthConverter$ * 6N   1  +System.Windows.Controls.DataGridBoundColumn	   Binding	 65   �� *Book+6N     5      $ Filme��6%   $ * 6N   	 64   �� *Movie+6N     5      $ Spiele��6%   $ * 6N   	 65   �� *Game+6N   5      5      5      