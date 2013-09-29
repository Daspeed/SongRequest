(function (React, reqwest, JSON, window) {
    'use strict';

    var convertNumberToTime = function (num) {
        var minutes = parseInt(num / 60, 10);
        var seconds = parseInt(num % 60, 10);

        return String(minutes) + ((seconds < 10) ? ':0' : ':') + String(seconds);
    };

    var fixer = function (num) {
        return (num < 10 ? '0' : '') + String(num);
    };

    var D = React.DOM;

    var ControlsPanel = React.createClass({
        next: function () {
            reqwest('/dynamic/next');
        },
        pause: function () {
            reqwest('/dynamic/pause');
        },
        rescan: function () {
            reqwest('/dynamic/rescan');
        },
        changeVolume: function () {
            var newVolume = prompt('New volume', String(this.props.volume));
            if (newVolume) {
                reqwest({
                    url: '/dynamic/volume',
                    method: 'post',
                    data: newVolume
                });
            }
        },
        render: function () {
            var me = this;
            var headerDiv = D.div({ className: 'headerDiv' }, D.h1({}, 'SongRequest ♫'));
            var controlsDiv = D.div({ className: 'controlsDiv' }, [
                D.input({ type: 'button', className: 'button nextButton', value: '►►', onClick: me.next }),
                D.input({ type: 'button', className: 'button pauseButton', value: '►', onClick: me.pause }),
                D.input({ type: 'button', className: 'button rescanButton', value: '↻', onClick: me.rescan }),
                D.input({ type: 'button', className: 'button volumeButton', value: 'Volume\r\n' + this.props.volume, onClick: me.changeVolume })
            ]);

            return D.div({ className: 'currentStatusDiv gradient' }, [headerDiv, controlsDiv]);
        }
    });

    var DisplayControl = React.createClass({
        render: function () {

            var songDescription = (this.props.RequestedSong.Song.Artist || 'Unknown') + ' - ' + (this.props.RequestedSong.Song.Name || 'Unknown');
            var timeDisplay = '('+convertNumberToTime(this.props.Position || 0) + '/' + convertNumberToTime(this.props.RequestedSong.Song.Duration || 0) +')';
            var requesterDescription = 'Requested by: ' + (this.props.RequestedSong.RequesterName || '');

            return D.div(
                { className: 'controlCombineDiv' },
                D.div(
                    { className: 'statusDiv' },
                    [
                        D.p(null, [
                            D.span({ className: 'statusArtistTitle' }, songDescription),
                            ' ',
                            D.span({ className: 'statusTime' }, timeDisplay)
                        ]),
                        D.p(null, [
                            D.span({ className: 'statusRequester' }, requesterDescription)
                        ])
                    ])
                );
        }
    });

    var QueueItem = React.createClass({
        removeFromQueue: function () {
            reqwest({
                url: '/dynamic/queue',
                method: 'delete',
                data: this.props.item.Song.TempId
            });
        },
        render: function () {
            var me = this;

            return D.div(
                { className: 'queueListDiv' },
                D.div({ className: 'queueItem' }, D.div({ className: this.props.className }, [
                    D.input({ type: 'button', className: 'button skipButton', value: '-', title: 'Remove song from queue', onClick: me.removeFromQueue }),
                    D.p(null, D.span({ className: 'queueRequester' }, 'ETA: ' + this.props.displayETA + ', requested by: ' + this.props.item.RequesterName)),
                    D.p(null, D.span({ className: 'queueArtist' }, (this.props.item.Song.Artist||'Unknown'))),
                    D.p(null, [
                        D.span({ className: 'queueTitle' }, (this.props.item.Song.Name||'Unknown')),
                        D.span({ className: 'queueTime' }, '(' + convertNumberToTime(this.props.item.Song.Duration || 0) + ')')
                    ])
                ]))
            );
        }
    });

    var QueueControl = React.createClass({
        render: function () {
            var me = this;

            var getHeaderText = function (queueLength, duration) {
                switch (queueLength) {
                    case 0:
                        return 'Queue | No songs';
                    case 1:
                        return 'Queue | 1 song';
                    default:
                        return 'Queue | ' + queueLength + ' songs (' + convertNumberToTime(duration) + ')';
                }
            }

            var queue = this.props.Queue;
            var controls = [];
            var item, containerClassName;
            var duration = 0;
            var ETA = (this.props.PlayerStatus.RequestedSong.Song.Duration || 0) - (this.props.PlayerStatus.Position || 0);
            for (var i = 0; i < queue.length; i += 1) {
                containerClassName = 'qitem ' +
                    (queue[i].RequesterName === this.props.Self ? 'self' : '') +
                    (i % 2 === 0 ? 'even' : 'odd') + 'queueitemcontainer';

                var displayETA = (function (baseDate) {
                    baseDate.setSeconds(baseDate.getSeconds() + ETA);
                    return fixer(baseDate.getHours()) + ':' + fixer(baseDate.getMinutes());
                })(new Date());

                item = QueueItem({ item: queue[i], 'displayETA': displayETA, className: containerClassName });

                duration += (queue[i].Song.Duration || 0);

                controls.push(item);

                ETA += (queue[i].Song.Duration || 0);
            }

            var header = D.div(
                { className: 'queueHeaderDiv gradient' },
                D.h2(null, getHeaderText(this.props.Queue.length, duration))
            );

            //prepend
            controls.unshift(header);

            return D.div({ className: 'queueDiv' }, controls);
        }
    });


    var SongsControl = React.createClass({
        getInitialState: function () {
            return {
                searchText: '',
                pageNumber: 1,
                totalPages: 0,
                sortBy: 'artist',
                ascending: true,
                songsForCurrentPage: []
            };
        },
        onSearchTextChange: function(e) {
            this.setState({ searchText: e.target.value });
        },
        onPageNumberChange: function(e) {
            this.setState({ pageNumber: (parseInt(e.target.value, 10)||1) });
        },
        onSearch: function () {
            var me = this;

            var message = {
                'Filter': this.state.searchText,
                'Page': this.state.pageNumber || 1,
                'SortBy': this.state.sortBy,
                'Ascending': this.state.ascending
            };

            reqwest({
                url: '/dynamic/playlist',
                method: 'post',
                contentType: 'application/json',
                type: 'json',
                data: JSON.stringify(message),
                success: function (resp) {
                    me.setState({
                        ascending: resp.Ascending,
                        pageNumber: resp.CurrentPage,
                        sortBy: resp.SortBy,
                        totalPages: resp.TotalPageCount,
                        songsForCurrentPage: resp.SongsForCurrentPage
                    });
                }
            });
        },
        onReset: function (e) {
            var me = this;

            this.setState({ searchText: '' }, function () { me.onSearch(); });
        },
        onPrevious: function (e) {
            var me = this;

            var newPage = Math.max(this.state.pageNumber - 1, 1);
            this.setState({ pageNumber: newPage }, function () { me.onSearch(); });
        },
        onNext: function (e) {
            var me = this;

            var newPage = Math.min(this.state.pageNumber + 1, this.state.totalPages);
            this.setState({ pageNumber: newPage }, function () { me.onSearch(); });
        },
        componentWillMount: function () {
            var me = this;

            me.onSearch();
        },
        headerClick: function(e){
            var me = this;
            var newSortBy = e.target.attributes['data-id'].value;

            me.setState({
                ascending: !me.state.ascending,
                sortBy: newSortBy
            }, function () { me.onSearch(); });
        },
        addToQueue: function(e){
            var tempId = e.target.attributes['data-id'].value;

            reqwest({
                url: '/dynamic/queue',
                method: 'post',
                type: 'json',
                data: tempId
            });
        },
        render: function () {
            var me = this;

            /* begin search */

            var prev = { type: 'button', className: 'button previousbutton', value: 'Previous', onClick: me.onPrevious };
            var next = { type: 'button', className: 'button nextbutton', value: 'Next', onClick: me.onNext };

            if (this.state.pageNumber <= 1)
                prev.disabled = true;

            if (this.state.pageNumber >= this.state.totalPages)
                next.disabled = true;

            var searchControl = D.div(
                { className: 'songsSearchDiv' }, [
                    D.form({ onSubmit: function (e) { e.preventDefault(); } }, [
                        D.input({ type: 'text', className: 'searchtext', value: this.state.searchText, onChange: me.onSearchTextChange }),
                        ' ',
                        D.input({ type: 'submit', className: 'button searchbutton', value: 'Search', onClick: me.onSearch }),
                        D.input({ type: 'button', className: 'button clearbutton', value: 'Reset', onClick: me.onReset })
                    ]),
                    D.form({ onSubmit: function (e) { e.preventDefault(); } }, [
                        'Page: ',
                        D.input({ type: 'text', className: 'pagetext', value: this.state.pageNumber, onChange: me.onPageNumberChange }),
                        ' of ',
                        D.span({ className: 'pageCount' }, this.state.totalPages)
                    ]),
                    ' ',
                    D.input(prev),
                    D.input(next)
                ]);

            /* end search */

            /* begin songs */

            var getHeaderClass = function (headerName) {
                if (headerName === me.state.sortBy)
                    return 'sorted' + (me.state.ascending ? ' Asc' : ' Desc');
                return '';
            };

            var header = D.tr({ className: 'gradient headerrow' }, [
                D.th({ className: 'actionColumn' }, 'Action'),
                D.th({ 'data-id': 'name', onClick: me.headerClick, className: 'songNameColumn ' + getHeaderClass('name') }, 'Name'),
                D.th({ 'data-id': 'artist', onClick: me.headerClick, className: 'songArtistColumn ' + getHeaderClass('artist') }, 'Artist'),
                D.th({ className: 'songDurationColumn' }, 'Length'),
                D.th({ 'data-id': 'genre', onClick: me.headerClick, className: 'songGenreColumn ' + getHeaderClass('genre') }, 'Genre'),
                D.th({ 'data-id': 'year', onClick: me.headerClick, className: 'songYearColumn ' + getHeaderClass('year') }, 'Year'),
                D.th({ 'data-id': 'date', onClick: me.headerClick, className: 'songDateCreatedColumn ' + getHeaderClass('date') }, 'Date added'),
                D.th({ 'data-id': 'playdate', onClick: me.headerClick, className: 'songDatePlayedColumn ' + getHeaderClass('playdate') }, 'Last played'),
                D.th({ className: 'songRequesterColumn' }, 'Requester'),
                D.th({ className: 'songSkippedByColumn' }, 'Skipped by')
            ]);

            var songs = this.state.songsForCurrentPage;
            var rows = [header];
            var song, row;

            for (var i = 0; i < songs.length; i += 1) {
                song = songs[i];

                row = D.tr({ className: 'songRow ' + ((i % 2 == 1) ? 'evenrow' : 'oddrow') }, [
                    D.td({ className: 'actionColumn' },
                        D.input({ 'data-id': song.TempId, type: 'button', className: 'button addButton', value: '+', title: 'Add song to queue', onClick: me.addToQueue })),
                    D.td({ className: 'songNameColumn' }, D.a({href: song.FileName, title: song.FileName}, song.Name)),
                    D.td({ className: 'songArtistColumn' }, song.Artist),
                    D.td({ className: 'songDurationColumn' }, convertNumberToTime(song.Duration)),
                    D.td({ className: 'songGenreColumn' }, song.Genre),
                    D.td({ className: 'songYearColumn' }, song.Year),
                    D.td({ className: 'songDateCreatedColumn' }, song.DateCreated),
                    D.td({ className: 'songDatePlayedColumn' }, song.LastPlayTime),
                    D.td({ className: 'songRequesterColumn' }, song.LastRequester),
                    D.td({ className: 'songSkippedByColumn' }, song.SkippedBy)
                ]);

                rows.push(row);
            }

            /* end songs */

            return D.div({ className: 'songsOuterDiv' }, [
                D.div({ className: 'songsHeaderDiv gradient' }, D.h2(null, 'Songs')),
                searchControl,
                D.div({ className: 'songsDiv' }, [
                    D.table({ className: 'songsTable' }, rows)
                ])
            ]);
        }
    });

    var SongRequest = React.createClass({
        getCurrentState: function () {
            var me = this;

            reqwest({
                url: '/dynamic/queue',
                method: 'get',
                type: 'json',
                success: function (resp) {
                    me.setState(resp);

                    var song = resp.PlayerStatus.RequestedSong.Song;

                    window.document.title = 'SongRequest | ' + (song.Artist || 'Unknown') + ' - ' + (song.Name || 'Unknown');
                }
            });
        },
        getInitialState: function () {
            return {
                PlayerStatus: {
                    Position: 0,
                    RequestedSong: {
                        Song: {}
                    },
                    Volume: 0
                },
                Queue: [],
                Self: ''
            };
        },
        componentWillMount: function () {
            var me = this;

            me.getCurrentState();
            setInterval(
              function () { me.getCurrentState() },
              1000
            );
        },
        render: function () {
            var me = this;

            var controlsPanel = ControlsPanel({ volume: this.state.PlayerStatus.Volume });
            var displayControl = DisplayControl(this.state.PlayerStatus);
            var queueControl = QueueControl(this.state);
            var songsControl = SongsControl();

            return D.div({ style: { padding: '0px' } }, [
                controlsPanel,
                displayControl,
                D.div({ className: 'outerCombineDiv' }, [
                    queueControl,
                    D.div({ className: 'combineDiv' },
                        songsControl)
                ])
            ]);
        }
    });

    React.renderComponent(SongRequest(), document.getElementById('app'));
})(React, reqwest, JSON, window);