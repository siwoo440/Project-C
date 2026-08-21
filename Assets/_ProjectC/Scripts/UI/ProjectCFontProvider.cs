using TMPro; // 텍스트 메시 기능 사용
using UnityEngine; // 유니티 리소스 기능 사용
public static class ProjectCFontProvider // 프로젝트 공용 한글 폰트 제공
{ // 클래스 시작
    private const string KoreanFontResourcePath = "Fonts/NotoSansCJKkr-Regular"; // 한글 폰트 리소스 경로
    private static TMP_FontAsset cachedKoreanFontAsset; // 생성된 한글 TMP 폰트
    public static TMP_FontAsset KoreanFontAsset // 한글 TMP 폰트 조회
    { // 속성 시작
        get // 폰트 조회 시작
        { // 조회 처리 시작
            if (cachedKoreanFontAsset != null) // 기존 한글 폰트 확인
            { // 기존 폰트 처리 시작
                return cachedKoreanFontAsset; // 기존 한글 폰트 반환
            } // 기존 폰트 처리 종료
            Font sourceFont = Resources.Load<Font>(KoreanFontResourcePath); // 원본 한글 폰트 불러오기
            if (sourceFont == null) // 원본 폰트 누락 확인
            { // 폰트 누락 처리 시작
                Debug.LogError($"[ProjectCFontProvider] 한글 폰트를 찾을 수 없습니다: Resources/{KoreanFontResourcePath}"); // 폰트 누락 출력
                return TMP_Settings.defaultFontAsset; // 기본 폰트 반환
            } // 폰트 누락 처리 종료
            cachedKoreanFontAsset = TMP_FontAsset.CreateFontAsset(sourceFont); // 동적 TMP 폰트 생성
            if (cachedKoreanFontAsset == null) // TMP 폰트 생성 결과 확인
            { // 생성 실패 처리 시작
                Debug.LogError("[ProjectCFontProvider] 한글 TMP 폰트 생성에 실패했습니다."); // 생성 실패 출력
                return TMP_Settings.defaultFontAsset; // 기본 폰트 반환
            } // 생성 실패 처리 종료
            cachedKoreanFontAsset.name = "NotoSansCJKkr Dynamic SDF"; // 동적 폰트 이름 설정
            RegisterGlobalFallback(cachedKoreanFontAsset); // 전역 대체 폰트 등록
            return cachedKoreanFontAsset; // 생성 한글 폰트 반환
        } // 조회 처리 종료
    } // 속성 종료
    private static void RegisterGlobalFallback(TMP_FontAsset fontAsset) // 전역 대체 폰트 등록
    { // 대체 폰트 등록 시작
        if (TMP_Settings.fallbackFontAssets == null) // 대체 폰트 목록 누락 확인
        { // 목록 누락 처리 시작
            return; // 대체 폰트 등록 중단
        } // 목록 누락 처리 종료
        if (!TMP_Settings.fallbackFontAssets.Contains(fontAsset)) // 기존 등록 여부 확인
        { // 신규 대체 폰트 처리 시작
            TMP_Settings.fallbackFontAssets.Add(fontAsset); // 전역 대체 폰트 추가
        } // 신규 대체 폰트 처리 종료
    } // 대체 폰트 등록 종료
} // 클래스 종료
