using System.Windows.Controls;
using DynamicData;
using TailBlazer.Views.Tail;

namespace TailBlazer.Infrastructure;

public interface IAttachedListBox
{
    void Receive(ListBox selector);
}


public interface ISelectionMonitor: IDisposable
{
    string GetSelectedText();

    string SelectedText => GetSelectedText();

    /// <summary>
    /// ������ ���� ��ġ ã��
    /// </summary>
    string GetSelectedTextFilePosition();

    /// <summary>
    /// irc Ŀ�ǵ带 �߸�
    /// </summary>
    string GetSelectedTextIrcCmd();

    /// <summary>
    /// ���� ���� ��������
    /// </summary>
    IEnumerable<string> GetSelectedItems();

    IObservableList<LineProxy> Selected { get; }
}