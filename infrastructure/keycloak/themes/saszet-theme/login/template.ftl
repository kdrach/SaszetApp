<#macro registrationLayout bodyClass="" displayInfo=false displayMessage=true displayRequiredFields=false>
<!DOCTYPE html>
<html lang="${(locale.currentLanguageTag)!''}">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>${msg("loginTitle",(realm.displayName!''))}</title>
    <#if properties.styles?has_content>
        <#list properties.styles?split(' ') as style>
            <link href="${url.resourcesPath}/${style}" rel="stylesheet" />
        </#list>
    </#if>
</head>
<body>

    <div class="saszet-card relative">
        <#if realm.internationalizationEnabled && locale?? && locale.supported?size > 1>
            <div class="absolute top-6 right-6 saszet-stagger-1 z-50">
                <div class="relative group">
                    <button type="button" class="flex items-center space-x-1 text-sm font-medium text-gray-700 bg-white px-3 py-1.5 rounded-md border border-gray-200 hover:bg-gray-50 transition-all cursor-pointer">
                        <span>${locale.current}</span>
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path></svg>
                    </button>
                    <div class="absolute right-0 mt-2 w-32 bg-white rounded-md shadow-lg border border-gray-100 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200 overflow-hidden">
                        <ul class="py-1 m-0 list-none px-0">
                            <#list locale.supported as l>
                                <li>
                                    <a href="${l.url}" class="block px-4 py-2 text-sm text-gray-700 hover:bg-emerald-50 hover:text-[#10B981] transition-colors" style="text-decoration: none;">${l.label}</a>
                                </li>
                            </#list>
                        </ul>
                    </div>
                </div>
            </div>
        </#if>

        <#if displayMessage && message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
            <div class="mb-6 p-3 rounded-md text-sm font-medium text-center ${message.type = 'success'?string('bg-green-50 text-green-800', message.type = 'error'?string('bg-red-50 text-red-800', 'bg-blue-50 text-blue-800'))}">
                ${kcSanitize(message.summary)?no_esc}
            </div>
        </#if>

        <#nested "form">
    </div>

</body>
</html>
</#macro>
