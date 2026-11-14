using System; // 인수 예외 사용
using UnityEngine; // 화면 객체와 텍스처 사용
using UnityEngine.UI; // 배경 이미지 사용
public static class LobbyBackgroundInstaller // 거점 배경 이미지 연결기
{ // 연결기 시작
    public static RawImage Install(Canvas canvas, Texture2D texture) // 캔버스에 배경 추가
    { // 배경 연결 시작
        if (canvas == null || texture == null) // 필수 인수 확인
        { // 인수 누락 처리 시작
            throw new ArgumentNullException(canvas == null ? nameof(canvas) : nameof(texture)); // 누락 인수 예외
        } // 인수 누락 처리 종료
        GameObject background = new GameObject("LobbyBackground", typeof(RectTransform), typeof(RawImage)); // 배경 화면 객체 생성
        RectTransform rect = background.GetComponent<RectTransform>(); // 배경 영역 조회
        rect.SetParent(canvas.transform, false); // 거점 캔버스 연결
        rect.SetAsFirstSibling(); // 다른 UI 뒤쪽 배치
        rect.anchorMin = Vector2.zero; // 화면 왼쪽 아래 고정
        rect.anchorMax = Vector2.one; // 화면 오른쪽 위 고정
        rect.offsetMin = Vector2.zero; // 왼쪽 아래 여백 제거
        rect.offsetMax = Vector2.zero; // 오른쪽 위 여백 제거
        RawImage image = background.GetComponent<RawImage>(); // 배경 이미지 조회
        image.texture = texture; // 생성한 배경 텍스처 연결
        image.raycastTarget = false; // 버튼 입력 차단 방지
        return image; // 연결된 배경 반환
    } // 배경 연결 종료
} // 연결기 종료
