using System.Collections; // 실제 전투 화면 대기 사용
using NUnit.Framework; // 전투 이미지 검사 사용
using UnityEngine; // 전투 화면과 이미지 사용
using UnityEngine.TestTools; // 플레이 모드 검사 사용
using UnityEngine.UI; // 전투 배경과 버튼 검사 사용
public sealed class BattleUiArtTests // 전투 화면 이미지 적용 검사
{ // 이미지 검사 시작
    [Test] // 생성 이미지와 모서리 배치 검사 지정
    public void GeneratedFrames_UseSlicedSpritesAndPreserveExistingOnMissing() // 실제 생성 이미지와 누락 시 기존 배경 유지 확인
    { // 이미지 적용 검사 시작
        GameObject target = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 독립 화면 이미지 생성
        Image image = target.GetComponent<Image>(); // 실제 화면 이미지 조회
        Assert.IsTrue(BattleUiArt.Apply(image, "HandPanel")); // 새 손패 배경 이미지 적용 확인
        Assert.AreEqual("HandPanel", image.sprite.texture.name); // 실제 생성 손패 이미지 확인
        Assert.AreEqual(Image.Type.Sliced, image.type); // 화면 크기 변경 시 패널 모서리 유지 확인
        Assert.Greater(image.sprite.border.x, 0f); // 손패 패널 모서리 범위 확인
        Assert.IsTrue(BattleUiArt.Apply(image, "CardFrame")); // 새 카드 프레임 이미지 적용 확인
        Assert.AreEqual("CardFrame", image.sprite.texture.name); // 실제 생성 카드 이미지 확인
        Sprite original = image.sprite; // 기존 정상 이미지 보관
        Assert.IsFalse(BattleUiArt.Apply(image, "Missing")); // 없는 이미지 적용 거부 확인
        Assert.AreSame(original, image.sprite); // 이미지 누락 시 기존 화면 보존 확인
        Object.DestroyImmediate(target); // 독립 화면 이미지 정리
    } // 이미지 적용 검사 종료
    [UnityTest] // 실제 손패와 카드 화면 검사 지정
    public IEnumerator BattleHandAndCards_UseGeneratedArtAndKeepStates() // 실제 카드 선택과 사용 불가 표현 유지 확인
    { // 실제 전투 화면 검사 시작
        yield return new EnterPlayMode(); // 실제 전투 화면 플레이 시작
        GameObject canvas = new GameObject("BattleUiCanvas", typeof(RectTransform), typeof(Canvas)); // 독립 전투 캔버스 생성
        Object.DontDestroyOnLoad(canvas); // 씬 전환 중 독립 전투 화면 유지
        GameObject handObject = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleHandView)); // 실제 손패 화면 생성
        handObject.transform.SetParent(canvas.transform, false); // 전투 캔버스에 손패 연결
        Image handImage = handObject.GetComponent<Image>(); // 실제 손패 배경 조회
        Assert.AreEqual("HandPanel", handImage.sprite.texture.name); // 실제 손패 배경에 생성 이미지 적용 확인
        Assert.IsNotNull(handObject.transform.Find("EndTurnButton").GetComponent<Image>().sprite); // 턴 종료 버튼의 이미지 적용 확인
        GameObject cardObject = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BattleCardView)); // 실제 카드 화면 생성
        cardObject.transform.SetParent(canvas.transform, false); // 전투 캔버스에 카드 연결
        BattleCardView card = cardObject.GetComponent<BattleCardView>(); // 실제 카드 화면 조회
        Image cardImage = cardObject.GetComponent<Image>(); // 실제 카드 배경 조회
        Assert.AreEqual("CardFrame", cardImage.sprite.texture.name); // 실제 카드에 생성 프레임 적용 확인
        Assert.GreaterOrEqual(cardObject.transform.Find("CardFace").GetComponent<Image>().color.a, 0.95f); // 겹친 카드 내용이 비치지 않는 바탕 확인
        Assert.IsTrue(cardObject.transform.Find("CardFace").GetComponent<CanvasGroup>().ignoreParentGroups); // 비활성 카드도 뒤쪽 내용을 가리는지 확인
        Color normalColor = cardImage.color; // 기본 카드 색상 기록
        card.SetSelected(true); // 실제 카드 선택 상태 적용
        Assert.AreNotEqual(normalColor, cardImage.color); // 카드 선택 상태 시각 구분 확인
        card.SetInteractable(false); // 실제 카드 사용 불가 상태 적용
        Assert.Less(cardObject.GetComponent<CanvasGroup>().alpha, 1f); // 사용 불가 카드 투명도 구분 확인
        Object.DestroyImmediate(canvas); // 독립 전투 화면 정리
        yield return new ExitPlayMode(); // 실제 전투 화면 플레이 종료
    } // 실제 전투 화면 검사 종료
} // 이미지 검사 종료
