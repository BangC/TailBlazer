using System.Collections.ObjectModel;
using System.Windows.Input;
using TailBlazer.Domain.Infrastructure;
using TailBlazer.Infrastructure;
using TailBlazer.Infrastructure.Virtualisation;

namespace TailBlazer.Views.Tail;

public interface ILinesVisualisation : IScrollReceiver, IDisposable
{
    ReadOnlyObservableCollection<LineProxy> Lines { get; }
    IProperty<int> Count { get; }
    IProperty<int> MaximumChars { get; }
    ICommand CopyToClipboardCommand { get; }
    /// <summary>
    /// ���� ��ġ ���� �޴� ���
    /// </summary>
    ICommand CopyFilePositionToClipboardCommand { get; }

    /// <summary>
    /// ���� ����� ��ü
    /// </summary>
    ISelectionMonitor SelectionMonitor { get; }

    /// <summary>
    /// 
    /// </summary>
    LineProxy SelectedItem { get; set; }

    TextScrollDelegate HorizonalScrollChanged { get; }
    int PageSize { get; set; }
    int FirstIndex { get; set; }
}