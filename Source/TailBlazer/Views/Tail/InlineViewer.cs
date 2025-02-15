using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using TailBlazer.Domain.Annotations;
using TailBlazer.Domain.FileHandling;
using TailBlazer.Domain.Formatting;
using TailBlazer.Domain.Infrastructure;
using TailBlazer.Infrastructure;
using TailBlazer.Infrastructure.Virtualisation;
using TailBlazer.Views.Formatting;

namespace TailBlazer.Views.Tail;

public class InlineViewer : AbstractNotifyPropertyChanged, ILinesVisualisation
{
    private readonly IDisposable _cleanUp;
    private readonly ReadOnlyObservableCollection<LineProxy> _data;
    private readonly ISubject<ScrollRequest> _userScrollRequested = new ReplaySubject<ScrollRequest>(1);
    private int _firstIndex;
    private int _pageSize;
    private bool _isSettingScrollPosition;

    public ReadOnlyObservableCollection<LineProxy> Lines => _data;
    public IProperty<int> Count { get; }
    public ICommand CopyToClipboardCommand { get; }
    /// <summary>
    /// 파일 위치 복사 메뉴 명령
    /// </summary>
    public ICommand CopyFilePositionToClipboardCommand { get; }
    public IProperty<int> MaximumChars { get; }
    public ISelectionMonitor SelectionMonitor { get; }

    private LineProxy _selectedLine;
    public LineProxy SelectedItem
    {
        get => _selectedLine;
        set => SetAndRaise(ref _selectedLine, value);
    }

    public InlineViewer([NotNull] IObservable<ILineProvider> lineProvider,
        [NotNull] IObservable<LineProxy> selectedChanged,
        [NotNull] IClipboardHandler clipboardHandler,
        [NotNull] ISchedulerProvider schedulerProvider, 
        [NotNull] ISelectionMonitor selectionMonitor,
        [NotNull] ILogger logger, 
        [NotNull] IThemeProvider themeProvider,
        [NotNull] ITextFormatter textFormatter,
        [NotNull] ILineMatches lineMatches)
    {
        if (lineProvider == null) throw new ArgumentNullException(nameof(lineProvider));
        if (selectedChanged == null) throw new ArgumentNullException(nameof(selectedChanged));
        if (clipboardHandler == null) throw new ArgumentNullException(nameof(clipboardHandler));
        if (schedulerProvider == null) throw new ArgumentNullException(nameof(schedulerProvider));
        if (themeProvider == null) throw new ArgumentNullException(nameof(themeProvider));
        SelectionMonitor = selectionMonitor ?? throw new ArgumentNullException(nameof(selectionMonitor));
        CopyToClipboardCommand = new Command(() => clipboardHandler.WriteToClipboard(selectionMonitor.GetSelectedText()));

        // 파일 위치 복사
        CopyFilePositionToClipboardCommand = new Command(() => clipboardHandler.WriteToClipboard(selectionMonitor.GetSelectedTextFilePosition()));

        _isSettingScrollPosition = false;
        var pageSize = this.WhenValueChanged(vm=>vm.PageSize);

        //if use selection is null, tail the file
        var scrollSelected = selectedChanged
            .CombineLatest(lineProvider, pageSize, (proxy, lp, pge) => proxy == null ? new ScrollRequest(pge,0) : new ScrollRequest(pge, proxy.Start))
            .DistinctUntilChanged();
            
        var horizonalScrollArgs = new ReplaySubject<TextScrollInfo>(1);
        HorizonalScrollChanged = hargs =>
        {
            horizonalScrollArgs.OnNext(hargs);
        };

        var scrollUser = _userScrollRequested
            .Where(x => !_isSettingScrollPosition)
            .Select(request => new ScrollRequest(ScrollReason.User, request.PageSize, request.FirstIndex));

        var scroller = scrollSelected.Merge(scrollUser)
            .ObserveOn(schedulerProvider.Background)
            .DistinctUntilChanged();


        var lineScroller = new LineScroller(lineProvider, scroller);
        Count = lineProvider.Select(lp=>lp.Count).ForBinding();
            
        MaximumChars = lineScroller.MaximumLines()
            .ObserveOn(schedulerProvider.MainThread)
            .ForBinding();


        var proxyFactory = new LineProxyFactory(textFormatter, lineMatches, horizonalScrollArgs.DistinctUntilChanged(), themeProvider);

        //load lines into observable collection
        var loader = lineScroller.Lines.Connect()
            .Transform(proxyFactory.Create)
            .Sort(SortExpressionComparer<LineProxy>.Ascending(proxy => proxy))
            .ObserveOn(schedulerProvider.MainThread)
            .Bind(out _data)
            .DisposeMany()
            .LogErrors(logger)
            .Subscribe();

        // track first visible index [required to set scroll extent]
        var firstIndexMonitor = lineScroller.Lines.Connect()
            .Buffer(TimeSpan.FromMilliseconds(250)).FlattenBufferResult()
            .ToCollection()
            .Select(lines => lines.Count == 0 ? 0 : lines.Select(l => l.Index).Max() - lines.Count + 1)
            .ObserveOn(schedulerProvider.MainThread)
            .Subscribe(first =>
            {
                try
                {
                    _isSettingScrollPosition = true;
                    FirstIndex = first;
                }
                finally
                {
                    _isSettingScrollPosition = false;
                }
            });

        _cleanUp = new CompositeDisposable(lineScroller,
            loader,
            Count,
            firstIndexMonitor,
            SelectionMonitor,
            MaximumChars,
            horizonalScrollArgs.SetAsComplete(),
            _userScrollRequested.SetAsComplete());
    }
        
    void IScrollReceiver.ScrollBoundsChanged(ScrollBoundsArgs boundsArgs)
    {
        if (boundsArgs == null) throw new ArgumentNullException(nameof(boundsArgs));
        if (!_isSettingScrollPosition)
            _userScrollRequested.OnNext(new ScrollRequest(ScrollReason.User, boundsArgs.PageSize, boundsArgs.FirstIndex));
        PageSize = boundsArgs.PageSize;
        FirstIndex = boundsArgs.FirstIndex;
    }

    void IScrollReceiver.ScrollChanged(ScrollChangedArgs scrollChangedArgs)
    {

    }

    public TextScrollDelegate HorizonalScrollChanged { get; }


    public void ScrollDiff(int lineChanged)
    {
        _userScrollRequested.OnNext(new ScrollRequest(ScrollReason.User, PageSize, FirstIndex + lineChanged));
    }

    public int PageSize
    {
        get { return _pageSize; }
        set { SetAndRaise(ref _pageSize, value); }
    }

    public int FirstIndex
    {
        get { return _firstIndex; }
        set { SetAndRaise(ref _firstIndex, value); }
    }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}