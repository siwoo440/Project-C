using System; // 예외 처리와 문자열 비교 사용
using System.IO; // OS 폰트 파일명 확인
using TMPro; // TextMesh Pro 폰트 에셋 사용
using UnityEngine; // 유니티 폰트와 리소스 사용
using UnityEngine.TextCore.LowLevel; // TMP 글리프 렌더 모드 사용

public static class ProjectCFontProvider // 프로젝트 공용 한글 폰트 제공
{
    private const string KoreanFontResourcePath = "Fonts/NotoSansCJKkr-Regular"; // 프로젝트 한글 폰트 리소스 경로
    private const string KoreanProbeText = "가나다라마바사아자차카타파하봄정신력탐사전투사망회복"; // 실제 한글 출력 가능 여부 검사 문자열
    private const int SamplingPointSize = 90; // 동적 SDF 샘플링 크기
    private const int AtlasPadding = 9; // 동적 SDF 글리프 간격
    private const int AtlasSize = 1024; // 동적 SDF 아틀라스 크기

    private static readonly string[] PreferredFontFileTokens =
    {
        "malgun", // Windows 맑은 고딕
        "notosanscjkkr", // Noto Sans CJK KR
        "notosanskr", // Noto Sans KR
        "applesdgothicneo", // macOS Apple SD Gothic Neo
        "nanum", // 나눔 계열
        "gulim", // Windows 굴림
        "dotum", // Windows 돋움
        "batang", // Windows 바탕
        "sourcehansans" // Source Han Sans
    }; // 우선 탐색할 한글 폰트 파일명 토큰

    private static TMP_FontAsset cachedKoreanFontAsset; // 준비 완료 한글 TMP 폰트
    private static Font cachedSourceFont; // 동적 TMP가 참조할 원본 Font 유지
    private static bool initializationAttempted; // 중복 초기화 방지

    public static TMP_FontAsset KoreanFontAsset // 한글 TMP 폰트 조회
    {
        get
        {
            if (cachedKoreanFontAsset != null)
            {
                return cachedKoreanFontAsset;
            }

            if (initializationAttempted)
            {
                return TMP_Settings.defaultFontAsset;
            }

            initializationAttempted = true;

            if (TryCreateFromProjectResource(out TMP_FontAsset resourceFontAsset))
            {
                cachedKoreanFontAsset = resourceFontAsset;
                CompleteInitialization("프로젝트 NotoSans 폰트");
                return cachedKoreanFontAsset;
            }

            if (TryCreateFromPreferredOsFontPath(out TMP_FontAsset osFontAsset))
            {
                cachedKoreanFontAsset = osFontAsset;
                CompleteInitialization("OS 한글 폰트");
                return cachedKoreanFontAsset;
            }

            Debug.LogError(
                "[ProjectCFontProvider] 한글 TMP 폰트를 준비할 수 없습니다. " +
                "프로젝트 폰트가 Git LFS 포인터이거나 OS 한글 폰트 파일을 찾지 못했습니다.");

            return TMP_Settings.defaultFontAsset;
        }
    }

    private static bool TryCreateFromProjectResource(out TMP_FontAsset fontAsset) // 프로젝트 Resources 폰트 시도
    {
        fontAsset = null;

        Font sourceFont =
            Resources.Load<Font>(KoreanFontResourcePath); // 프로젝트 원본 폰트 조회

        if (sourceFont == null)
        {
            return false;
        }

        return TryCreateValidatedFontAsset(
            sourceFont,
            out fontAsset); // 실제 한글 글리프 생성 가능 여부까지 검증
    }

    private static bool TryCreateFromPreferredOsFontPath(out TMP_FontAsset fontAsset) // OS 폰트 파일 경로 기반 한글 폰트 생성
    {
        fontAsset = null;

        string[] fontPaths;

        try
        {
            fontPaths =
                Font.GetPathsToOSFonts(); // 설치된 OS 폰트의 실제 파일 경로 조회
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[ProjectCFontProvider] OS 폰트 경로 조회 실패: {exception.Message}");

            return false;
        }

        if (fontPaths == null ||
            fontPaths.Length == 0)
        {
            return false;
        }

        for (int tokenIndex = 0;
             tokenIndex < PreferredFontFileTokens.Length;
             tokenIndex++)
        {
            string token =
                PreferredFontFileTokens[tokenIndex]; // 현재 우선 폰트 토큰

            for (int pathIndex = 0;
                 pathIndex < fontPaths.Length;
                 pathIndex++)
            {
                string fontPath =
                    fontPaths[pathIndex]; // 현재 OS 폰트 파일 경로

                if (!IsPreferredFontPath(
                        fontPath,
                        token))
                {
                    continue;
                }

                if (TryCreateValidatedFontAssetFromPath(
                        fontPath,
                        out fontAsset))
                {
                    Debug.Log(
                        $"[ProjectCFontProvider] OS 한글 폰트 적용: {Path.GetFileName(fontPath)}");

                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsPreferredFontPath(
        string fontPath,
        string token) // 우선 한글 폰트 파일 여부 확인
    {
        if (string.IsNullOrWhiteSpace(fontPath) ||
            string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string fileName =
            Path.GetFileNameWithoutExtension(fontPath); // 확장자 제외 파일명 조회

        return !string.IsNullOrWhiteSpace(fileName) &&
               fileName.IndexOf(
                   token,
                   StringComparison.OrdinalIgnoreCase) >= 0; // 우선 폰트 토큰 포함 여부 반환
    }

    private static bool TryCreateValidatedFontAssetFromPath(
        string fontPath,
        out TMP_FontAsset fontAsset) // 실제 OS 폰트 파일에서 TMP 생성
    {
        fontAsset = null;

        if (string.IsNullOrWhiteSpace(fontPath))
        {
            return false;
        }

        try
        {
            Font sourceFont =
                new Font(fontPath); // 이름이 아닌 OS 폰트 실제 파일 경로로 Font 생성

            if (sourceFont == null)
            {
                return false;
            }

            if (!TryCreateValidatedFontAsset(
                    sourceFont,
                    out fontAsset))
            {
                return false;
            }

            cachedSourceFont =
                sourceFont; // 동적 글리프 추가에 필요한 원본 Font 수명 유지

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[ProjectCFontProvider] OS 폰트 변환 실패: " +
                $"{Path.GetFileName(fontPath)} / {exception.Message}");

            return false;
        }
    }

    private static bool TryCreateValidatedFontAsset(
        Font sourceFont,
        out TMP_FontAsset fontAsset) // Font를 동적 TMP로 만들고 한글 글리프 검증
    {
        fontAsset = null;

        if (sourceFont == null)
        {
            return false;
        }

        try
        {
            TMP_FontAsset candidate =
                TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    SamplingPointSize,
                    AtlasPadding,
                    GlyphRenderMode.SDFAA,
                    AtlasSize,
                    AtlasSize,
                    AtlasPopulationMode.Dynamic,
                    true); // 다중 아틀라스 가능한 동적 SDF 생성

            if (candidate == null)
            {
                return false;
            }

            bool added =
                candidate.TryAddCharacters(
                    KoreanProbeText,
                    out string missingCharacters,
                    false); // 실제 한글 글리프를 아틀라스에 추가해 검증

            if (!added ||
                !string.IsNullOrEmpty(missingCharacters) ||
                !candidate.HasCharacters(KoreanProbeText))
            {
                return false;
            }

            candidate.name =
                "ProjectC Korean Dynamic SDF"; // 런타임 한글 폰트 이름 지정

            fontAsset =
                candidate; // 검증 완료 폰트 반환

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[ProjectCFontProvider] TMP 동적 폰트 생성 실패: {exception.Message}");

            return false;
        }
    }

    private static void CompleteInitialization(string sourceDescription) // 한글 폰트 등록 마무리
    {
        RegisterGlobalFallback(
            cachedKoreanFontAsset); // 전역 fallback 등록

        RegisterDefaultFontFallback(
            cachedKoreanFontAsset); // LiberationSans 등 기본 TMP의 직접 fallback 등록

        Debug.Log(
            $"[ProjectCFontProvider] 한글 TMP 폰트 준비 완료: {sourceDescription}");
    }

    private static void RegisterGlobalFallback(TMP_FontAsset fontAsset) // TMP 전역 fallback 등록
    {
        if (fontAsset == null ||
            TMP_Settings.fallbackFontAssets == null)
        {
            return;
        }

        if (!TMP_Settings.fallbackFontAssets.Contains(fontAsset))
        {
            TMP_Settings.fallbackFontAssets.Add(
                fontAsset); // 전역 fallback 목록 추가
        }
    }

    private static void RegisterDefaultFontFallback(TMP_FontAsset fontAsset) // 기본 LiberationSans 계열에도 한글 fallback 연결
    {
        TMP_FontAsset defaultFontAsset =
            TMP_Settings.defaultFontAsset; // 현재 TMP 기본 폰트 조회

        if (fontAsset == null ||
            defaultFontAsset == null ||
            defaultFontAsset == fontAsset ||
            defaultFontAsset.fallbackFontAssetTable == null)
        {
            return;
        }

        if (!defaultFontAsset.fallbackFontAssetTable.Contains(fontAsset))
        {
            defaultFontAsset.fallbackFontAssetTable.Add(
                fontAsset); // 기존 기본 폰트를 쓰는 텍스트도 한글 fallback 사용
        }
    }
}
