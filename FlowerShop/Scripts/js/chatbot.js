/**
 * FLOWER BOUTIQUE - AI FLORIST CHATBOT WIDGET
 */

document.addEventListener('DOMContentLoaded', function () {
    const triggerBtn = document.getElementById('chatbotTriggerBtn');
    const chatbotWindow = document.getElementById('chatbotWindow');
    const closeBtn = document.getElementById('chatbotCloseBtn');
    const chatBody = document.getElementById('chatbotBody');
    const chatInput = document.getElementById('chatbotInput');
    const sendBtn = document.getElementById('chatbotSendBtn');
    const quickChips = document.querySelectorAll('.js-chat-chip');

    if (!triggerBtn || !chatbotWindow) return;

    // Toggle Chat Window
    triggerBtn.addEventListener('click', function () {
        chatbotWindow.classList.toggle('active');
        if (chatbotWindow.classList.contains('active')) {
            setTimeout(() => chatInput.focus(), 300);
        }
    });

    if (closeBtn) {
        closeBtn.addEventListener('click', function () {
            chatbotWindow.classList.remove('active');
        });
    }

    // Quick Action Chips
    quickChips.forEach(chip => {
        chip.addEventListener('click', function () {
            const promptText = this.getAttribute('data-prompt');
            if (promptText) {
                sendUserMessage(promptText);
            }
        });
    });

    // Send on Button Click
    if (sendBtn) {
        sendBtn.addEventListener('click', function () {
            handleSend();
        });
    }

    // Send on Enter Key
    if (chatInput) {
        chatInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                handleSend();
            }
        });
    }

    function handleSend() {
        const text = chatInput.value.trim();
        if (text) {
            sendUserMessage(text);
            chatInput.value = '';
        }
    }

    function appendMessage(text, isUser = false) {
        const bubble = document.createElement('div');
        bubble.className = `chat-bubble ${isUser ? 'chat-bubble-user' : 'chat-bubble-bot'}`;
        
        // Simple Markdown bold & newline parser
        let formatted = text
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/\n/g, '<br/>');
            
        bubble.innerHTML = formatted;
        chatBody.appendChild(bubble);
        chatBody.scrollTop = chatBody.scrollHeight;
        return bubble;
    }

    function sendUserMessage(message) {
        appendMessage(message, true);

        // Hiển thị trạng thái đang soạn tin (typing)
        const typingIndicator = document.createElement('div');
        typingIndicator.className = 'chat-bubble chat-bubble-bot';
        typingIndicator.id = 'chatTyping';
        typingIndicator.innerHTML = '<i class="fa-solid fa-spinner fa-spin" style="margin-right: 6px; color: #c84b61;"></i> <em>Florist đang tư vấn...</em>';
        chatBody.appendChild(typingIndicator);
        chatBody.scrollTop = chatBody.scrollHeight;

        fetch('/Chatbot/SendMessage', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: message })
        })
        .then(res => res.json())
        .then(data => {
            const typing = document.getElementById('chatTyping');
            if (typing) typing.remove();

            if (data.success) {
                appendMessage(data.reply, false);

                // Nếu có sản phẩm gợi ý
                if (data.products && data.products.length > 0) {
                    const productsBox = document.createElement('div');
                    productsBox.style.marginTop = '6px';
                    data.products.forEach(p => {
                        productsBox.innerHTML += `
                            <div class="chat-product-card">
                                <img src="${p.ImageUrl}" />
                                <div style="flex: 1;">
                                    <div class="chat-product-title">${p.Name}</div>
                                    <div class="chat-product-price">${new Intl.NumberFormat('vi-VN').format(p.Price)} đ</div>
                                </div>
                                <a href="/hoa/${p.Slug}" class="btn-quick-add" style="padding: 4px 10px; font-size: 0.75rem;">Xem</a>
                            </div>
                        `;
                    });
                    chatBody.appendChild(productsBox);
                }

                // Nếu có thẻ tra cứu đơn hàng
                if (data.orderSummary) {
                    const orderCard = document.createElement('div');
                    orderCard.className = 'chat-order-card';
                    orderCard.innerHTML = `
                        <div class="chat-order-status-badge">${data.orderSummary.StatusName}</div>
                        <div style="font-size: 0.85rem; font-weight: 700; color: #2e2426; margin-bottom: 4px;">Mã đơn: ${data.orderSummary.OrderCode}</div>
                        <div style="font-size: 0.8rem; color: #666; margin-bottom: 8px;">Dự kiến giao: ${data.orderSummary.DeliveryTime}</div>
                        <a href="/OrderTracking?orderCode=${data.orderSummary.OrderCode}" class="btn btn-primary btn-sm" style="width: 100%; text-align: center; display: block; font-size: 0.8rem;">Xem Chi Tiết & Ảnh Hoa Thực Tế</a>
                    `;
                    chatBody.appendChild(orderCard);
                }

                chatBody.scrollTop = chatBody.scrollHeight;
            }
        })
        .catch(err => {
            const typing = document.getElementById('chatTyping');
            if (typing) typing.remove();
            appendMessage('Dạ em xin lỗi, đường truyền hơi bận, bạn vui lòng nhắn lại giúp em nhé!', false);
        });
    }
});
