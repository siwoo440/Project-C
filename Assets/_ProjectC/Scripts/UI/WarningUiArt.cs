using UnityEngine; // 경고 이미지 리소스 사용
using UnityEngine.UI; // 화면 이미지 사용

public static class WarningUiArt // 공통 경고창 이미지 제공
{ // 경고 이미지 제공 시작
    private static Sprite backdrop; // 공통 경고 배경 스프라이트
    private static Sprite button; // 공통 경고 버튼 스프라이트
    public static void ApplyBackdrop(Image target) // 경고 배경 이미지 적용
    { // 경고 배경 적용 시작
        if (backdrop == null) // 경고 배경 준비 여부 확인
        { // 경고 배경 준비 시작
            backdrop = CreateSprite("UI/SaveMenu/WarningBackdrop"); // 제작 배경 스프라이트 생성
        } // 경고 배경 준비 종료
        Apply(target, backdrop); // 경고 배경 이미지 연결
    } // 경고 배경 적용 종료
    public static void ApplyButton(Image target) // 경고 버튼 이미지 적용
    { // 경고 버튼 적용 시작
        if (button == null) // 경고 버튼 준비 여부 확인
        { // 경고 버튼 준비 시작
            button = CreateSprite("UI/SaveMenu/WarningButton"); // 제작 버튼 스프라이트 생성
        } // 경고 버튼 준비 종료
        Apply(target, button); // 경고 버튼 이미지 연결
    } // 경고 버튼 적용 종료
    private static Sprite CreateSprite(string path) // 이미지 리소스로 스프라이트 생성
    { // 스프라이트 생성 시작
        Sprite imported = Resources.Load<Sprite>(path); // 단일 스프라이트 리소스 조회
        if (imported != null) // 가져온 스프라이트 존재 확인
        { // 가져온 이미지 사용 시작
            return imported; // 가져온 스프라이트 반환
        } // 가져온 이미지 사용 종료
        Texture2D texture = Resources.Load<Texture2D>(path); // 제작 이미지 조회
        if (texture == null) // 이미지 리소스 존재 확인
        { // 리소스 누락 처리 시작
            Debug.LogWarning("[WarningUI] 이미지를 찾을 수 없습니다: " + path); // 누락 이미지 안내
            return null; // 이미지 없음 반환
        } // 리소스 누락 처리 종료
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)); // 전체 이미지 스프라이트 생성
        sprite.hideFlags = HideFlags.HideAndDontSave; // 런타임 공용 이미지 보관 설정
        return sprite; // 생성 스프라이트 반환
    } // 스프라이트 생성 종료
    private static void Apply(Image target, Sprite sprite) // 대상 화면 이미지 설정
    { // 이미지 설정 시작
        if (target == null || sprite == null) // 대상과 스프라이트 존재 확인
        { // 이미지 연결 불가 처리 시작
            return; // 기존 배경 유지
        } // 이미지 연결 불가 처리 종료
        target.sprite = sprite; // 공용 이미지 연결
        target.color = Color.white; // 이미지 원본 색상 유지
    } // 이미지 설정 종료
} // 경고 이미지 제공 종료
