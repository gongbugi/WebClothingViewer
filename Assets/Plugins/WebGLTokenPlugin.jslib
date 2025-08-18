mergeInto(LibraryManager.library, {

  GetTokenFromLocalStorage: function () {
    try {
      // 다양한 토큰 키를 시도
      var token = window.localStorage.getItem('token') || 
                  window.localStorage.getItem('authToken') || 
                  window.localStorage.getItem('accessToken') ||
                  window.localStorage.getItem('jwtToken') ||
                  window.localStorage.getItem('bearerToken');
      
      if (token) {
        // Unity에서 읽을 수 있도록 힙에 문자열 할당
        var bufferSize = lengthBytesUTF8(token) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(token, buffer, bufferSize);
        return buffer;
      }
      
      // 토큰이 없으면 빈 문자열 반환
      var emptyString = '';
      var bufferSize = lengthBytesUTF8(emptyString) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(emptyString, buffer, bufferSize);
      return buffer;
      
    } catch (e) {
      console.error('Error getting token from localStorage:', e);
      
      // 오류 시 빈 문자열 반환
      var emptyString = '';
      var bufferSize = lengthBytesUTF8(emptyString) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(emptyString, buffer, bufferSize);
      return buffer;
    }
  }

});