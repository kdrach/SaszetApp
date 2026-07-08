<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('firstName','lastName','email','username','password','password-confirm'); section>
    <#if section = "header">
        ${msg("registerTitle")}
    <#elseif section = "form">
    <div id="kc-form" class="bg-white p-8 rounded-2xl shadow-md max-w-sm w-full mx-auto mt-10">
        <h1 class="text-2xl font-bold mb-6 text-center text-gray-800">SaszetApp</h1>
        <form id="kc-register-form" class="${properties.kcFormClass!}" action="${url.registrationAction}" method="post">
            <div class="${properties.kcFormGroupClass!} mb-4">
                <div class="${properties.kcLabelWrapperClass!}">
                    <label for="username" class="${properties.kcLabelClass!} block text-sm font-medium text-gray-700 mb-1">${msg("username")}</label>
                </div>
                <div class="${properties.kcInputWrapperClass!}">
                    <input type="text" id="username" class="${properties.kcInputClass!} mt-1 block w-full px-3 py-2 bg-white border border-gray-300 rounded-md text-sm shadow-sm placeholder-gray-400 focus:outline-none focus:border-[#10B981] focus:ring-1 focus:ring-[#10B981]" name="username" value="${(register.formData.username!'')}" autocomplete="username" />
                </div>
            </div>

            <div class="${properties.kcFormGroupClass!} mb-4">
                <div class="${properties.kcLabelWrapperClass!}">
                    <label for="email" class="${properties.kcLabelClass!} block text-sm font-medium text-gray-700 mb-1">${msg("email")}</label>
                </div>
                <div class="${properties.kcInputWrapperClass!}">
                    <input type="text" id="email" class="${properties.kcInputClass!} mt-1 block w-full px-3 py-2 bg-white border border-gray-300 rounded-md text-sm shadow-sm placeholder-gray-400 focus:outline-none focus:border-[#10B981] focus:ring-1 focus:ring-[#10B981]" name="email" value="${(register.formData.email!'')}" autocomplete="email" />
                </div>
            </div>

            <#if passwordRequired??>
                <div class="${properties.kcFormGroupClass!} mb-4">
                    <div class="${properties.kcLabelWrapperClass!}">
                        <label for="password" class="${properties.kcLabelClass!} block text-sm font-medium text-gray-700 mb-1">${msg("password")}</label>
                    </div>
                    <div class="${properties.kcInputWrapperClass!}">
                        <input type="password" id="password" class="${properties.kcInputClass!} mt-1 block w-full px-3 py-2 bg-white border border-gray-300 rounded-md text-sm shadow-sm placeholder-gray-400 focus:outline-none focus:border-[#10B981] focus:ring-1 focus:ring-[#10B981]" name="password" autocomplete="new-password"/>
                    </div>
                </div>

                <div class="${properties.kcFormGroupClass!} mb-6">
                    <div class="${properties.kcLabelWrapperClass!}">
                        <label for="password-confirm" class="${properties.kcLabelClass!} block text-sm font-medium text-gray-700 mb-1">${msg("passwordConfirm")}</label>
                    </div>
                    <div class="${properties.kcInputWrapperClass!}">
                        <input type="password" id="password-confirm" class="${properties.kcInputClass!} mt-1 block w-full px-3 py-2 bg-white border border-gray-300 rounded-md text-sm shadow-sm placeholder-gray-400 focus:outline-none focus:border-[#10B981] focus:ring-1 focus:ring-[#10B981]" name="password-confirm" autocomplete="new-password" />
                    </div>
                </div>
            </#if>

            <div class="${properties.kcFormGroupClass!}">
                <div id="kc-form-options" class="${properties.kcFormOptionsClass!}">
                    <div class="${properties.kcFormOptionsWrapperClass!} mb-4 text-center">
                        <span><a href="${url.loginUrl}" class="text-sm font-medium text-[#10B981] hover:text-emerald-500">${kcSanitize(msg("backToLogin"))?no_esc}</a></span>
                    </div>
                </div>

                <div id="kc-form-buttons" class="${properties.kcFormButtonsClass!}">
                    <input class="${properties.kcButtonClass!} ${properties.kcButtonPrimaryClass!} ${properties.kcButtonBlockClass!} ${properties.kcButtonLargeClass!} w-full flex justify-center py-2 px-4 border border-transparent rounded-xl shadow-sm text-sm font-medium text-white bg-[#10B981] hover:bg-emerald-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-[#10B981] cursor-pointer" type="submit" value="${msg("doRegister")}"/>
                </div>
            </div>
        </form>
    </div>
    </#if>
</@layout.registrationLayout>
