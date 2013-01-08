function SongRequestController($scope, $http, $timeout) {

    $scope.queue = [];
    $scope.self = '';
    $scope.playerStatus = {};

    $scope.sortBy = 'artist';
    $scope.ascending = true;
    $scope.searchResult = [];
    $scope.currentPage = 1;
    $scope.filter = '';
    $scope.pageCount = 0;
    $scope.playlistStyle = {};

    var convertNumberToTime = function (num) {
        var minutes = parseInt(num / 60, 10);
        var seconds = parseInt(num % 60, 10);

        return String(minutes) + ((seconds < 10) ? ':0' : ':') + String(seconds);
    };
    $scope.convertNumberToTime = convertNumberToTime;

    var fixer = function (num) {
        return (num < 10 ? '0' : '') + String(num);
    };

    var digitize = function (arg) {
        var allDigits = String(arg).replace(/\D/g, '');
        if (allDigits && allDigits.length > 0)
            return parseInt(allDigits, 10);
        else
            return 0;
    };

    var enrichQueue = function (queue) {
        if (!$scope.playerStatus || !$scope.playerStatus.RequestedSong)
            return [];

        var acc = Math.max(0, $scope.playerStatus.RequestedSong.Song.Duration - ($scope.playerStatus.Position || 0));

        angular.forEach(queue, function (value) {
            value.ETA = acc;
            acc += $scope.playerStatus.RequestedSong.Song.Duration;
            value.Till = (function (baseDate) {
                baseDate.setSeconds(baseDate.getSeconds() + value.ETA);
                return fixer(baseDate.getHours()) + ':' + fixer(baseDate.getMinutes());
            })(new Date());
        });

        return queue;
    };

    var refreshQueue = function () {
        $http({ method: 'GET', url: '/dynamic/queue' }).success(function (data) {
            $scope.queue = enrichQueue(data.Queue);
            $scope.self = data.Self;
            $scope.playerStatus = data.PlayerStatus;
        });
    };

    var getPlayList = function () {
        var message = {
            'Filter': $scope.filter,
            'Page': digitize($scope.currentPage) || 1,
            'SortBy' : $scope.sortBy,
            'Ascending' : $scope.ascending };

        $http({ method: 'POST', url: '/dynamic/playlist', data: message }).success(function (data) {
            var currentPage = Math.max(data.CurrentPage, 1);
            var totalPageCount = Math.max(data.TotalPageCount, 1);

            $scope.sortBy = data.SortBy,
            $scope.ascending = data.Ascending;
            $scope.searchResult = data.SongsForCurrentPage;
            $scope.currentPage = data.CurrentPage;
            $scope.pageCount = Math.max(data.TotalPageCount, 1);
        });
    };

    $scope.getSongName = function (song) {
        if (song.Artist && song.Name)
            return song.Artist + ' - ' + song.Name;
        else
            return song.Artist || song.Name || 'Unknown';
    };

    $scope.getCurrentSongName = function () {
        if (!$scope.playerStatus)
            return '';

        if (!$scope.playerStatus.RequestedSong)
            return '';

        var requestedSong = $scope.playerStatus.RequestedSong.Song;

        return $scope.getSongName(requestedSong);
    };

    $scope.getCurrentSongPosition = function () {
        if (!$scope.playerStatus)
            return convertNumberToTime(0);

        return convertNumberToTime($scope.playerStatus.Position || 0);
    };

    $scope.getCurrentSongDuration = function () {
        if (!$scope.playerStatus)
            return convertNumberToTime(0);

        if (!$scope.playerStatus.RequestedSong)
            return convertNumberToTime(0);

        return convertNumberToTime($scope.playerStatus.RequestedSong.Song.Duration);
    };

    $scope.getCurrentSongRequester = function () {
        if (!$scope.playerStatus)
            return this.convertNumberToTime();

        if (!$scope.playerStatus.RequestedSong)
            return '';

        return $scope.playerStatus.RequestedSong.RequesterName;
    };

    $scope.getVolume = function(){
        if (!$scope.playerStatus)
            return '0';

        return $scope.playerStatus.Volume;
    }

    $scope.getQueueDuration = function () {
        return convertNumberToTime(
            _.chain($scope.queue)
            .map(function (requestedSong) { return requestedSong.Song.Duration; })
            .reduce(function (state, value) { return state + value; }, 0)
            .value());
    };

    $scope.getQueueItemClass = function (item, index) {
        return 'qitem ' +
            (item.RequesterName === $scope.self ? 'self' : '') +
            (index % 2 === 0 ? 'even' : 'odd') +
            'queueitemcontainer';
    };

    $scope.changeVolume = function () {
        var newVolume = prompt('New volume', String($scope.playerStatus.Volume));
        if (newVolume) {
            $http({ method: 'POST', url: '/dynamic/volume', data: newVolume }).success(function (volume) {
                $scope.playerStatus.Volume = volume;
            });
        }
    };

    $scope.next = function () {
        $http({ method: 'GET', url: '/dynamic/next' });
    };

    $scope.pause = function () {
        $http({ method: 'GET', url: '/dynamic/pause' });
    };

    $scope.rescan = function () {
        $http({ method: 'GET', url: '/dynamic/rescan' });
    };

    $scope.addToQueue = function (song) {
        $http({ method: 'POST', url: '/dynamic/queue', data: song.TempId }).success(function () {
            refreshQueue();
        });
    };

    $scope.removeFromQueue = function (song) {
        $http({ method: 'DELETE', url: '/dynamic/queue', data: song.TempId }).success(function () {
            refreshQueue();
        });
    };

    $scope.search = getPlayList;
    $scope.clear = function () {
        $scope.filter = '';
        $scope.currentPage = 1;
        $scope.search();
    };

    $scope.previousPage = function () {
        $scope.currentPage--;
        $scope.search();
    };

    $scope.nextPage = function () {
        $scope.currentPage++;
        $scope.search();
    };

    $scope.getHeaderClass = function (headerName) {
        if (headerName === $scope.sortBy)
            return 'sorted' + ($scope.ascending ? ' Asc' : ' Desc');
        return '';
    };

    $scope.sort = function (sortBy) {
        $scope.ascending = !$scope.ascending;
        $scope.sortBy = sortBy;

        $scope.search();
    };

    $scope.getPlaylistWidth = function () {
        return Math.max(670, $(window).width() - $('#queue').width() - 50);
    }
    $scope.$watch($scope.getPlaylistWidth, function (newValue, oldValue) {
        $scope.playlistStyle = { width: String(newValue) + 'px' };
    });
    $(window).on('resize', function () {
        $scope.$apply();
    });

    refreshQueue();
    getPlayList();

    (function () {
        function poll() {
            refreshQueue();
            $timeout(poll, 1000);
        };
        poll();
    })();
}