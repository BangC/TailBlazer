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
    /// 선택의 파일 위치 찾기
    /// </summary>
    string GetSelectedTextFilePosition();

    /// <summary>
    /// irc 커맨드를 추림
    /// </summary>
    string GetSelectedTextIrcCmd();

    /// <summary>
    /// 선택 라인 가져오기
    /// </summary>
    IEnumerable<string> GetSelectedItems();

    IObservableList<LineProxy> Selected { get; }
}