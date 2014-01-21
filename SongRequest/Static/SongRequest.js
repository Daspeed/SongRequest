(function () {
    angular
        .module('SongRequest', [])
        .config(function ($compileProvider) {
            $compileProvider.urlSanitizationWhitelist(/^\s*file:/);
        })
        .controller('SongRequestController', function ($scope, $http, $timeout) {
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

            var convertRating = function (num) {
                if (num === 10)
                    return '★★★★★';
                if (num === 9)
                    return '★★★★☆';
                if (num === 8)
                    return '★★★★';
                if (num === 7)
                    return '★★★☆';
                if (num === 6)
                    return '★★★';
                if (num === 5)
                    return '★★☆';
                if (num === 4)
                    return '★★';
                if (num === 3)
                    return '★☆';
                if (num === 2)
                    return '★';
                if (num === 1)
                    return '☆';

                return '';
            };
            $scope.convertRating = convertRating;

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
                    acc += value.Song.Duration;
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

            $scope.getPlayList = function () {
                var message = {
                    'Filter': $scope.filter,
                    'Page': digitize($scope.currentPage) || 1,
                    'SortBy': $scope.sortBy,
                    'Ascending': $scope.ascending
                };

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

            $scope.getSongArtist = function (song) {
                return song.Artist || 'Unknown artist';
            };

            $scope.getSongName = function (song) {
                return song.Name || 'Unknown title';
            };

            $scope.getSongGenre = function (song) {
                return song.Genre || 'Unknown genre';
            };

            $scope.getSongAlbum = function (song) {
                return song.Album || 'Unknown album';
            };

            $scope.getCurrentSongArtist = function () {
                if (!$scope.playerStatus)
                    return '';

                if (!$scope.playerStatus.RequestedSong)
                    return '';

                var requestedSong = $scope.playerStatus.RequestedSong.Song;

                return $scope.getSongArtist(requestedSong);
            };

            $scope.getCurrentSongName = function () {
                if (!$scope.playerStatus)
                    return '';

                if (!$scope.playerStatus.RequestedSong)
                    return '';

                var requestedSong = $scope.playerStatus.RequestedSong.Song;

                return $scope.getSongName(requestedSong);
            };

            $scope.getCurrentSongFileName = function () {
                if (!$scope.playerStatus)
                    return '';

                if (!$scope.playerStatus.RequestedSong)
                    return '';

                return $scope.playerStatus.RequestedSong.Song.FileName;
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

            $scope.getCurrentSongGenre = function () {
                if (!$scope.playerStatus)
                    return '';

                if (!$scope.playerStatus.RequestedSong)
                    return '';

                var requestedSong = $scope.playerStatus.RequestedSong.Song;

                return $scope.getSongGenre(requestedSong);
            };

            $scope.getCurrentSongRating = function () {
                if (!$scope.playerStatus)
                    return '';

                if (!$scope.playerStatus.RequestedSong)
                    return '';

                var requestedSong = $scope.playerStatus.RequestedSong.Song;

                return $scope.convertRating(requestedSong.Rating);
            };

            $scope.getCurrentSongAlbum = function () {
                if (!$scope.playerStatus)
                    return '';

                if (!$scope.playerStatus.RequestedSong)
                    return '';

                var requestedSong = $scope.playerStatus.RequestedSong.Song;

                return $scope.getSongAlbum(requestedSong);
            };

            $scope.getCurrentSongRequester = function () {
                if (!$scope.playerStatus)
                    return this.convertNumberToTime();

                if (!$scope.playerStatus.RequestedSong)
                    return '';

                return $scope.playerStatus.RequestedSong.RequesterName;
            };

            $scope.getCurrentSongId = function () {
                if (!$scope.playerStatus)
                    return this.convertNumberToTime();

                if (!$scope.playerStatus.RequestedSong)
                    return '';

                if (!$scope.playerStatus.RequestedSong.Song.TempId)
                    return '';

                return $scope.playerStatus.RequestedSong.Song.TempId;
            };

            $scope.getVolume = function () {
                if (!$scope.playerStatus)
                    return '0';

                return $scope.playerStatus.Volume;
            }

            $scope.getQueueDuration = function () {
                var totalDuration = 0;
                var i = 0;
                var queueLength = $scope.queue.length;

                for (i = 0; i < queueLength; i++) {
                    totalDuration += $scope.queue[i].Song.Duration;
                }

                return convertNumberToTime(totalDuration);
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

            $scope.search = function () {
                $scope.currentPage = 1;
                $scope.getPlayList();
            };

            $scope.clear = function () {
                $scope.filter = '';
                $scope.currentPage = 1;
                $scope.getPlayList();
            };

            $scope.previousPage = function () {
                $scope.currentPage--;
                $scope.getPlayList();
            };

            $scope.nextPage = function () {
                $scope.currentPage++;
                $scope.getPlayList();
            };

            $scope.getHeaderClass = function (headerName) {
                if (headerName === $scope.sortBy)
                    return 'sorted' + ($scope.ascending ? ' Asc' : ' Desc');
                return '';
            };

            $scope.sort = function (sortBy) {
                $scope.ascending = !$scope.ascending;
                $scope.sortBy = sortBy;
                $scope.getPlayList();
            };

            refreshQueue();
            $scope.getPlayList();

            (function () {
                function poll() {
                    refreshQueue();
                    $timeout(poll, 1000);
                };
                poll();
            })();
        })
})();