using NUnit.Framework; // 테스트 기능 사용
using UnityEngine; // 리소스와 화면 객체 사용
using UnityEngine.UI; // 배경 이미지 컴포넌트 사용
public sealed class LobbyBackgroundTests // 거점 배경 이미지 테스트
{ // 테스트 클래스 시작
    [Test] // 테스트 지정
    public void BackgroundResource_CreatesNonInteractiveFullScreenImage() // 전체 화면 배경 연결 확인
    { // 테스트 시작
        Texture2D texture = Resources.Load<Texture2D>("UI/LobbyBackground"); // 거점 배경 이미지 조회
        Assert.IsNotNull(texture); // 배경 이미지 존재 확인
        GameObject canvasObject = new GameObject("BackgroundTest", typeof(RectTransform), typeof(Canvas)); // 테스트 캔버스 생성
        Canvas canvas = canvasObject.GetComponent<Canvas>(); // 캔버스 컴포넌트 조회
        RawImage image = LobbyBackgroundInstaller.Install(canvas, texture); // 배경 이미지 연결
        Assert.AreSame(texture, image.texture); // 생성 이미지의 텍스처 확인
        Assert.IsFalse(image.raycastTarget); // 배경의 입력 차단 방지 확인
        Assert.AreEqual(Vector2.zero, image.rectTransform.anchorMin); // 화면 시작점 확인
        Assert.AreEqual(Vector2.one, image.rectTransform.anchorMax); // 화면 끝점 확인
        Object.DestroyImmediate(canvasObject); // 테스트 화면 정리
    } // 테스트 종료
} // 테스트 클래스 종료
