# 카페 경영 시뮬레이션 — 부족점 분석 & 개선 로드맵

> GPGP(Good Pizza, Great Pizza)류 요리·경영 게임 벤치마킹 + 현재 코드 전수 감사(게임성·비주얼·리소스 주입성) 기반.
> 작성: 다중 에이전트 분석(벤치마크/게임성/비주얼/리소스). 모든 항목은 코드 근거(`파일:라인`) 동반.
> 원칙: `.cs`만 수정, 리소스는 **유니티 인스펙터 주입 + Resources 폴백 + 절차적 폴백** 3단으로 라우팅(무회귀).

---

## 0. 요약 — 가장 비중 있는 부족점

| 순위 | 영역 | 한 줄 진단 |
|---|---|---|
| ★★★ | **死에셋(리소스 미연결)** | 이미 만든 BGM·손님 캐릭터·커피/재료 스프라이트·픽셀 음식 팩이 **잘못된 경로** 때문에 화면/소리에 전혀 안 나옴 |
| ★★★ | **인스펙터 주입성** | 손님 캐릭터·배경·BGM·메뉴/재료 아이콘·음료색·UI 테마색이 인스펙터로 못 끼움(하드코드/절차) |
| ★★★ | **손님 비주얼** | 손님 얼굴/표정/등장·퇴장 연출이 전무 → 감정 이입·피드백 채널 0 |
| ★★ | **보상감 연출** | 정밀 계산된 서빙 점수·정산이 텍스트 한 줄 → 별점/등급/카운트업 없음 |
| ★★ | **진행/경제** | 코인 소비처가 발주뿐, 업그레이드·목표·파산·엔딩 부재 → 장기 동기 0 |
| ★★ | **콘텐츠 천장** | 메뉴 8종·Day3 해금 종료, 난이도 일차 무관 상수 → 반복성 붕괴 |
| ★ | **죽은 변수** | 아이스/리드/specialTags가 채점 미반영 |

---

## 1. 🔴 死에셋 — 이미 만든 리소스가 코드 경로 불일치로 미사용 (최우선)

`Assets/Resources/`에 실제 에셋이 있으나 코드가 다른 경로/이름을 조회 → 전부 폴백으로 빠짐.

| 에셋(실재) | 코드가 찾는 경로 | 결과 | 해결 |
|---|---|---|---|
| `Bgm/CoffeeMachineSound·SuccessSound·WaterSound·ButtonClickSound.mp3` | `Audio/{key}` | 14 SFX 전부 절차음 | 키→파일명 매핑 폴백 |
| `Bgm/MP_Background.mp3` | (재생 코드 없음) | BGM 무음 | 루프 AudioSource 추가 |
| `Coffee/CoffeeMachine0/1, CoffeeBean, Coffee0/1` | `Sprites/Stations/{name}` | 머신/컵 색폴백 | name→경로 매핑 |
| `Ingredients/milk·syrup·cream·cup·sugar·strawberry·fruitmix·chocolatechips...` | `Sprites/Stations/{name}` | 재료 색폴백 | 재료명→경로 매핑 |
| `Characters/NPCBody(1-3)·NPCHead(1-3)·Face(angry/default/happy)` | (사용 코드 없음) | 손님 캐릭터 무표시 | CustomerCardUI 캐릭터 표시 |
| `karsiori/Pixel Art Food Pack/*`, `Foods/*` | (사용 코드 없음) | 픽셀 음식 아이콘 미사용 | 메뉴 아이콘 매핑 |
| `Fonts/*SDF`(6종) | `Pretendard`만 사용 | 폰트 위계 단조 | 타이틀/본문 폰트 분리 |

**조치**: `AudioManager.Resolve`/`CafeSpriteUtility.Station`/`ProceduralIconUtility`에 **인스펙터 → Resources 다중경로 → 절차** 라우팅. 임포트 타입이 Sprite/AudioClip이 아니면 로드 실패하므로, 사용자는 인스펙터 슬롯에 직접 끼우는 경로도 함께 제공.

---

## 2. 인스펙터 주입형 리소스 체계 (`CafeAssetConfig` 확장)

빈 GameObject에 붙이는 `CafeAssetConfig` 한 곳에서 거의 모든 리소스를 드래그-드롭으로 지정. 미지정 시 Resources→절차/하드코드 폴백.

**추가 슬롯(전부 `[SerializeField]`)**
- 손님: `Sprite[] customerBodies / customerHeads`, `Sprite faceHappy / faceDefault / faceAngry`
- 배경: `Sprite serviceBackground / mainMenuBackground`
- 메뉴 아이콘: `(메뉴명 → Sprite)` 맵 + `GetMenuIcon(name)`
- 재료 아이콘: `(IngredientType → Sprite)` 맵 + `GetIngredientIcon(type)`
- 음료 레이어 색: `espresso / milk / syrup / topping / iceTint`
- UI 테마색: `panelBg / primaryButton / successButton / dangerButton / patienceHigh / Mid / Low / gaugeNormal / gaugeSweet / dialLow / dialGreen / dialRed`
- 오디오: `AudioClip bgm` + `bgmVolume`, (기존 14 SFX 슬롯 유지)

**라우팅 지점**: `ProceduralIconUtility.GetMenuIcon/GetIngredientIcon`, `AudioManager.Resolve`, `CafeSpriteUtility.Station`, `CupCanvasUI`(색), 각 UI 팩토리(테마색)가 `CafeAssetConfig.Instance?.X ?? 기본값` 패턴으로 조회.

---

## 3. 게임성 부족점

### 3.1 진행 / 경제 / 업그레이드 (★★ critical)
- **현황**: 코인 소비처가 발주뿐(`SupplyManager`), `ClosingManager.AdvanceToNextDay`는 `CurrentDay++`만. 파산/엔딩/목표 없음(`Coin=Max(0,..)`). 업그레이드/상점/꾸미기 grep 무매치.
- **개선**: `UpgradeManager` + 업그레이드(추출 안전구역 확장·인내심 증가·대기열 슬롯·신규 스테이션). 가게 성장. 주차/시즌 목표 + 파산 리스크 + 엔딩. 영구 상태는 `SaveLoadUtility` 직렬화.
- **노력**: L

### 3.2 손님/주문 다양성 (★★ high)
- **현황**: `Customer`는 Id/Order/Patience/State뿐. 인내심 `Random 42~60s` 균일. `specialTags` 저장만 되고 채점 미반영. VIP/진상/단골/러시아워 없음.
- **개선**: 손님 유형(서두름·큰손·까다로움)별 인내심·단가·수정자. 특이 주문 수정자('우유 적게','샷 추가','휘핑 빼고')를 채점 반영. 능동 이벤트(러시아워·단체).
- **노력**: M~L

### 3.3 난이도 곡선 (★★ high)
- **현황**: `CustomerManager`의 손님 수(8)·도착(13~27)·인내심(42~60)·대기열(4) 전부 일차 무관 상수.
- **개선**: `CurrentDay` 비례 스케일(손님↑·도착간격↓·인내심↓).
- **노력**: M

### 3.4 동시처리 (★★ high)
- **현황**: `_activeCustomer` 단일 슬롯 + 단일 `_snapshot`, 손님 전환 시 빌드 폐기.
- **개선**: 손님별 스냅샷 보존(멀티-컵)으로 병렬 제작.
- **노력**: L

### 3.5 영업 보상(팁/서빙 콤보) (★ medium)
- **현황**: 수령액은 단순 곱(`ServiceManager.CalculatePayout`). 콤보는 머지에만.
- **개선**: 연속 완벽 서빙 콤보, 빠른 서빙/높은 인내심 팁.
- **노력**: M

### 3.6 죽은 변수 정합성 (★ medium)
- **현황**: `requiresIce/HasIce/HasLid/specialTags`가 `MenuCatalog` 채점에서 미사용(shot/milk/syrup/topping/forbidden만).
- **개선**: 아이스/휘핑 태그를 채점·메뉴 정체성에 반영(또는 정리).
- **노력**: S

### 3.7 온보딩 / 엔드게임 (medium)
- **현황**: 정적 도움말 1장(`HelpUIController`). 종료/도전과제/통계(`SaveData.totalServed` 미사용) 없음.
- **개선**: 단계별 튜토리얼, 누적 통계·도전과제·기록.
- **노력**: L

---

## 4. 비주얼 부족점

### 4.1 손님 캐릭터/표정 (★★ high) — 에셋 이미 존재
- **현황**: `CustomerCardUI`가 손님 자리에 음료 아이콘만. 감정은 인내심 바 색뿐.
- **개선**: NPCBody+NPCHead+Face(happy/default/angry)를 인내심·상태에 매핑해 표시.
- **노력**: M (에셋 존재로 단축)

### 4.2 손님 등장/퇴장 연출 (high)
- **현황**: `RebuildBinding`이 카드 즉시 `SetActive`. 슬라이드/페이드 없음.
- **개선**: 진입 슬라이드+페이드, 퇴장 트윈, arrive/leave 사운드 동기화.
- **노력**: M

### 4.3 정산 화면 (★★ high)
- **현황**: `ClosingUIController.ShowSettlement` 정적 텍스트 한 덩어리.
- **개선**: 코인·순이익 카운트업, 등급 스탬프 펀치, 성공률 막대, 순차 페이드.
- **노력**: M

### 4.4 서빙 결과 별점/등급 (★★ high)
- **현황**: 40/30/15/15 점수가 텍스트 한 줄로만 전달.
- **개선**: 서빙 직후 별 1~3 + 항목별 막대 + 완벽 시 강조 연출.
- **노력**: M

### 4.5 음료 컵 렌더 (medium)
- **현황**: 단색 2밴드 + 단색 원 점, 채움 즉시 점프, 뚜껑 단색.
- **개선**: crema/foam, 그라데이션, 얼음 형태, 채움 트윈, 토핑 종류별 형태.
- **노력**: L

### 4.6 상태 전환 / 버튼 피드백 / 주스 (medium~low)
- 화면 전환 하드 토글 → CanvasGroup 페이드.
- 버튼 ColorBlock/클릭음 없음 → `UIFactoryUtility.CreateButton` 단일점 추가.
- 인내심 임박/콤보/서빙 실패 시각 연출 없음 → 펄스/흔들림/플래시.

### 4.7 색/테마 일관성 (medium)
- **현황**: 인내심 색·패널색이 파일마다 중복 하드코드.
- **개선**: `CafeTheme` 정적 팔레트 + 인내심 색 공용 헬퍼(+ 인스펙터 오버라이드).
- **노력**: M

### 4.8 배경/공간/idle (medium)
- 단색 카운터 한 장 → 배경 레이어 + idle 모션(머신 김).

### 4.9 아이콘/폰트/가독성 (low)
- 절차 아이콘 64px AA 없음 → 128px+AA. 폰트 위계. 코인 천단위 포맷. 텍스트 아웃라인.

---

## 5. 사운드
- **현황**: 이벤트 SFX 6종(대부분 절차음), BGM/앰비언스 없음.
- **개선**: BGM 루프(`MP_Background`), 콤보 피치 상승, 카페 앰비언스.

---

## 6. 구현 단계(권장 순서)

- **Phase A (이번 차수, 최우선)**: 死에셋 연결 + `CafeAssetConfig` 대확장(인스펙터 주입) + BGM 재생 + 손님 캐릭터/표정 + `CafeTheme` + 버튼 ColorBlock/클릭음.
- **Phase B**: 서빙 결과 별점 팝업 + 정산 카운트업 + 손님 등장/퇴장 + 상태전환 페이드 + 죽은 변수(아이스/태그) 채점.
- **Phase C**: 난이도 곡선 + 영업 콤보/팁 + 손님 유형/수정자.
- **Phase D (대형)**: 업그레이드/상점 + 목표/파산/엔딩 + 멀티-컵 동시처리 + 튜토리얼 + 컵 렌더 고급화.

---

## 7. 인스펙터 셋업 안내(사용자 작업)
1. 빈 GameObject(예: `Managers`)에 `CafeAssetConfig` 추가.
2. 슬롯에 `Resources/Characters`, `Coffee`, `Ingredients`, `Bgm`, `karsiori`의 에셋을 드래그. (미지정 시 코드가 Resources 경로로 자동 폴백 시도)
3. PNG는 **Texture Type = Sprite (2D and UI)** 로 임포트되어야 `Resources.Load<Sprite>`가 동작. 안 보이면 임포트 설정 확인.
4. `ScriptableObject/Menu` 폴더는 없음 — 메뉴는 코드 폴백 8종으로 동작(원하면 폴더+`MenuRecipeSO` 추가 시 자동 합류).
